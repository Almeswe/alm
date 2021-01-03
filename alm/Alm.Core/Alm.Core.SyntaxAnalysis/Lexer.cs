﻿using System.IO;
using System.Collections.Generic;

using alm.Other.Structs;

using static alm.Other.Enums.TokenType;

namespace alm.Core.SyntaxAnalysis
{
    internal sealed class Lexer
    {
        private const int  EOF = -1;
        private const char chEOF = '\0';
        private const char chSpace = ' ';
        private const char chNewLn = '\n';
        private const char chCarrRet = '\r';

        private int charPos;
        private int linePos;

        public char currentChar;

        private int currentTokenIndex;
        private int currentCharIndex;
        private TextReader reader;

        private bool tokensCreated = false;

        public List<Token> tokens = new List<Token>();

        public Token CurrentToken => Peek(0);
        public Token PreviousToken => Peek(-1);

        public Lexer(string path)
        {
            reader = new StreamReader(path);
            currentCharIndex = -1;
            currentTokenIndex = -1;
            charPos = 0;
            linePos = 1;
        }

        public Token GetNextToken()
        {
            Token token;
            currentTokenIndex++;
            if (tokensCreated)
            {
                if (currentTokenIndex < tokens.Count)
                    token = tokens[currentTokenIndex];
                else
                    token = new Token(tkEOF, new Position(charPos, charPos, linePos));
                return token;
            }
            else
            {
                token = Token.GetNullToken();
                return token;
            }
        }
        public Token Peek(int offset)
        {
            if (currentTokenIndex + offset < tokens.Count) return tokens[currentTokenIndex + offset];
            else if (currentTokenIndex + offset < 0) return new Token(tkNull);
            else return new Token(tkEOF, new Position(charPos, charPos, linePos));
        }
        public Token[] GetTokens()
        {
            GetNextChar();
            while (currentChar != chEOF)
            {
                if (Match('"'))
                {
                    AddDoubleQuoteToken();
                    tokens.Add(RecognizeString());
                    AddDoubleQuoteToken();
                }
                else if (Match('\''))
                {
                    AddSingleQuoteToken();
                    tokens.Add(RecognizeSingleChar());
                    AddSingleQuoteToken();
                }
                else if (CharForNumber())
                    tokens.Add(RecognizeConst());
                else if (CharForIdentifier(true))
                    tokens.Add(RecognizeIdentifier());
                else if (char.IsWhiteSpace(currentChar))
                    GetNextChar();
                else
                {
                    tokens.Add(RecognizeChar());
                    GetNextChar();
                }
            }
            reader.Close();
            if (!tokensCreated) tokensCreated = true;
            return tokens.ToArray();
        }

        private Token RecognizeSingleChar()
        {
            Token token = new Token(tkCharConst, new Position(charPos - 1, charPos + 1, linePos),currentChar.ToString());
            GetNextChar();
            return token;
        }

        private Token RecognizeConst()
        {
            string num = string.Empty;
            int start = charPos;
            bool dot  = false;
            while (CharForNumber())
            {
                if (Match('.'))
                {
                    if (dot) break;
                    else dot = true;
                    num += ',';
                }
                else
                    num += currentChar.ToString();
                currentCharIndex++;
                GetNextChar();
            }
            int end = charPos;
            if (dot)
                return new Token(tkFloatConst, new Position(start, end, linePos), num);
            else 
                return new Token(tkIntConst, new Position(start, end, linePos), num);
        }

        private Token RecognizeIdentifier()
        {
            string ident = string.Empty;
            int start    = charPos;
            while (CharForIdentifier())
            {
                ident += currentChar.ToString();
                currentCharIndex++;
                GetNextChar();
            }
            int end = charPos;
            if (!Token.IsNull(GetReservedWord(ident))) return GetReservedWord(ident);
            return new Token(tkId, new Position(start, end, linePos), ident);
        }

        private void AddDoubleQuoteToken()
        {
            if (Match('"'))
            {
                tokens.Add(new Token(tkDQuote, new Position(charPos - 1, charPos, linePos)));
                GetNextChar();
            }
        }

        private void AddSingleQuoteToken()
        {
            if (Match('\''))
            {
                tokens.Add(new Token(tkSQuote, new Position(charPos - 1, charPos, linePos)));
                GetNextChar();
            }
        }

        private Token RecognizeString()
        {
            string str = string.Empty;
            int line = linePos;
            int start = charPos;
            while (CharForString(line))
            {
                str += currentChar.ToString();
                currentCharIndex++;
                GetNextChar();
            }
            return new Token(tkStringConst, new Position(start, start+str.Length, line), str);
        }

        private Token RecognizeChar()
        {
            switch (currentChar)
            {
                case '=':
                    if (reader.Peek() == '=')
                    {
                        GetNextChar();
                        return new Token(tkEqual, new Position(charPos, charPos+2, linePos));
                    }
                    return new Token(tkAssign, new Position(charPos, charPos+1, linePos));

                case '!':
                    if (reader.Peek() == '=')
                    {
                        GetNextChar();
                        return new Token(tkNotEqual, new Position(charPos, charPos+2, linePos));
                    }
                    return new Token(tkNull);

                case '<':
                    if (reader.Peek() == '=')
                    { 
                        GetNextChar();
                        return new Token(tkEqualLess, new Position(charPos, charPos + 2, linePos));
                    }
                    return new Token(tkLess, new Position(charPos, charPos + 1, linePos));

                case '>':
                    if (reader.Peek() == '=') 
                    {
                        GetNextChar(); 
                        return new Token(tkEqualGreater, new Position(charPos, charPos + 2, linePos));
                    }
                    return new Token(tkGreater, new Position(charPos, charPos + 1, linePos));

                case '+':
                    if (reader.Peek() == '=')
                    {
                        GetNextChar();
                        return new Token(tkAddAssign, new Position(charPos, charPos + 2, linePos));
                    }
                    return new Token(tkPlus,  new Position(charPos, charPos + 1, linePos));

                case '-':
                    if (reader.Peek() == '=')
                    {
                        GetNextChar();
                        return new Token(tkSubAssign, new Position(charPos, charPos + 2, linePos));
                    }
                    return new Token(tkMinus, new Position(charPos, charPos + 1, linePos));

                case '*':
                    if (reader.Peek() == '=')
                    {
                        GetNextChar();
                        return new Token(tkMultAssign, new Position(charPos, charPos + 2, linePos));
                    }
                    return new Token(tkMult,  new Position(charPos, charPos + 1, linePos));

                case '/':
                    if (reader.Peek() == '=')
                    {
                        GetNextChar();
                        return new Token(tkDivAssign, new Position(charPos, charPos + 2, linePos));
                    }
                    return new Token(tkDiv,   new Position(charPos, charPos + 1, linePos));

                case '@': return new Token(tkAt,        new Position(charPos, charPos + 1, linePos));
                case ';': return new Token(tkSemicolon, new Position(charPos, charPos + 1, linePos));
                case ':': return new Token(tkColon,     new Position(charPos, charPos + 1, linePos));
                case '(': return new Token(tkLpar,      new Position(charPos, charPos + 1, linePos));
                case ')': return new Token(tkRpar,      new Position(charPos, charPos + 1, linePos));

                case '{':  return new Token(tkLbra,  new Position(charPos, charPos + 1, linePos));
                case '}':  return new Token(tkRbra,  new Position(charPos, charPos + 1, linePos));
                case '"':  return new Token(tkDQuote, new Position(charPos, charPos + 1, linePos));
                case '\'': return new Token(tkDQuote, new Position(charPos, charPos + 1, linePos));
                case ',':  return new Token(tkComma, new Position(charPos, charPos + 1, linePos));
                case chEOF: return new Token(tkEOF, new Position(charPos, charPos + 1, linePos));
                default: return new Token(tkNull);
            }
        }

        private Token GetReservedWord(string word)
        {
            switch (word)
            {
                case "while": return new Token(tkWhile, new Position(charPos - 5, charPos, linePos));
                case "do":    return new Token(tkDo,    new Position(charPos - 2, charPos, linePos));
                case "if":    return new Token(tkIf,    new Position(charPos - 2, charPos, linePos));
                case "else":  return new Token(tkElse,  new Position(charPos - 4, charPos, linePos));

                case "not": return new Token(tkNot, new Position(charPos - 3, charPos, linePos));
                case "or":  return new Token(tkOr,  new Position(charPos - 2, charPos, linePos));
                case "and": return new Token(tkAnd, new Position(charPos - 3, charPos, linePos));

                case "func": return new Token(tkFunc, new Position(charPos - 4, charPos, linePos));
                case "of":   return new Token(tkOf,   new Position(charPos - 2, charPos, linePos));

                case "void":    return new Token(tkType, new Position(charPos - 4, charPos, linePos), "void");
                case "char":    return new Token(tkType, new Position(charPos - 4, charPos, linePos), "char");
                case "float":   return new Token(tkType, new Position(charPos - 5, charPos, linePos), "float");
                case "string":  return new Token(tkType, new Position(charPos - 6, charPos, linePos), "string");
                case "boolean": return new Token(tkType, new Position(charPos - 7, charPos, linePos), "boolean");
                case "integer": return new Token(tkType, new Position(charPos - 7, charPos, linePos), "integer");

                case "import": return new Token(tkImport, new Position(charPos - 6, charPos, linePos));
                case "global": return new Token(tkGlobal, new Position(charPos - 6, charPos, linePos));

                case "external": return new Token(tkExternalProp, new Position(charPos - 8, charPos, linePos));

                case "return": return new Token(tkRet,          new Position(charPos - 6, charPos, linePos));
                case "true":   return new Token(tkBooleanConst, new Position(charPos - 4, charPos, linePos), "true");
                case "false":  return new Token(tkBooleanConst, new Position(charPos - 5, charPos, linePos), "false");

                default: return Token.GetNullToken();
            }
        }

        private void GetNextChar()
        {
            if (reader.Peek() == EOF)
                currentChar = chEOF;
            else
            {
                currentChar = (char)reader.Read();
                charPos++;
                currentCharIndex++;
                if (currentChar == chNewLn)
                {
                    charPos = 0;
                    linePos++;
                    GetNextChar();
                }
            }
            CheckCommentary();
        }

        private void CheckCommentary()
        {
            int line = linePos;
            if (currentChar == '/')
            {
                //Проверка на однострочный комментарий
                if (reader.Peek() == '/')
                {
                    reader.Read();
                    while (linePos == line)
                        GetNextChar();
                }
                //Проверка на многострочный комментарий
                if (reader.Peek() == '*')
                {
                    reader.Read();
                    while (currentChar != '*' || reader.Peek() != '/')
                        GetNextChar();
                    GetNextChar();
                    GetNextChar();
                }
            }

        }

        public bool CharForIdentifier(bool isFirstSymbol = false)
        {
            if (char.IsDigit(currentChar))
                if (!isFirstSymbol)
                    return true;
            if (char.IsLetter(currentChar) || Match('_'))
                return true;
            return false;
        }

        public bool CharForNumber()
        {
           return (char.IsDigit(currentChar) || Match('.')) ? true : false;
        }

        public bool CharForString(int linePos,bool firstQuote = false)
        {
            if (Match('"') && !firstQuote) 
                return false;
            if (this.linePos != linePos)
                return false;
            return true;
        }

        public bool Match(char ch)
        {
            return currentChar == ch ? true : false;
        }
    }
}