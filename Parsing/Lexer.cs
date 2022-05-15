using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Milt.Parsing
{
    public struct Token
    {
        public TokenType type /*= TokenType.ERROR*/;
        public string lexeme; // corresponding text in the source file
        public object value;
        public int line;

        //  exact token position in source file
        //public int startIndex, length;

        public Token(TokenType type, string lexeme, object value, int line)
        {
            this.type   = type;
            this.lexeme = lexeme;
            this.value  = value;
            this.line   = line;
        }

        public override string ToString()
        {
            return $"{type} {lexeme} {value}";
        }
    }

    public enum TokenType
    {
        //  Starts at 256 to allow ascii chars
        //  to use their index as an enum value.

        //  Literal values
        IDENT = 256,
        NUMBER,
        STRING,

        PAREN_LEFT,
        PAREN_RIGHT,
        BRACE_LEFT,    // {
        BRACE_RIGHT,
        BRACKET_LEFT,  // [
        BRACKET_RIGHT,

        COMMA,
        PERIOD,
        UNDERSCORE,
        COLON,
        SEMICOLON,
        QUESTION,

        PLUS,
        MINUS,
        SLASH,
        STAR,
        PERCENT,

        //  1 or 2 length tokens
        EQUAL, EQUAL_EQUAL,
        BANG, BANG_EQUAL,
        LESS, LESS_EQUAL,
        GREATER, GREATER_EQUAL,

        //  Keywords
        TRUE, FALSE,
        IF, ELSE,
        WHILE, FOR,
        AND, OR,
        FUNC, RETURN,
        STRUCT, THIS,
        //VAR,
        NULL,


        EOF,
        ERROR,
    }


    public static class Lexer
    {
        private static string source;
        private static List<Token> tokens = new List<Token>();

        static int currentLexemeStart = 0;
        static int currentIndex = 0;
        static int currentLine = 1;

        static Dictionary<string, TokenType> keywords = new Dictionary<string, TokenType>()
        {
            { "true", TokenType.TRUE },
            { "false", TokenType.FALSE },
            { "if", TokenType.IF },
            { "else", TokenType.ELSE },
            { "while", TokenType.WHILE },
            { "for", TokenType.FOR },
            { "and", TokenType.AND },
            { "or", TokenType.OR },
            { "func", TokenType.FUNC },
            { "return", TokenType.RETURN },
            { "struct", TokenType.STRUCT },
            { "this", TokenType.THIS },
            { "null", TokenType.NULL },
        };

        public static List<Token> Lex(string text)
        {
            source = text;
            tokens.Clear();
            currentIndex = 0; 
            currentLine = 0; 

            while (!AtEnd())
            {
                currentLexemeStart = currentIndex;

                EatToken();
            }

            tokens.Add(new Token(TokenType.EOF, "", null, currentLine));
            return tokens;
        }

        private static void EatToken()
        {
            char c = EatChar();
            switch (c)
            {
                case '(': AddToken(TokenType.PAREN_LEFT); break;
                case ')': AddToken(TokenType.PAREN_RIGHT); break;
                case '{': AddToken(TokenType.BRACE_LEFT); break;
                case '}': AddToken(TokenType.BRACE_RIGHT); break;
                case '[': AddToken(TokenType.BRACKET_LEFT); break;
                case ']': AddToken(TokenType.BRACKET_RIGHT); break;

                case ',': AddToken(TokenType.COMMA); break;
                case '.': AddToken(TokenType.PERIOD); break;
                case '_': AddToken(TokenType.UNDERSCORE); break;
                case ':': AddToken(TokenType.COLON); break;
                case ';': AddToken(TokenType.SEMICOLON); break;
                case '?': AddToken(TokenType.QUESTION); break;

                case '+': AddToken(TokenType.PLUS); break;
                case '-': AddToken(TokenType.MINUS); break;
                case '*': AddToken(TokenType.STAR); break;
                case '%': AddToken(TokenType.PERCENT); break;

                case '!': AddToken(MatchNext('=') ? TokenType.BANG_EQUAL : TokenType.BANG); break;
                case '=': AddToken(MatchNext('=') ? TokenType.EQUAL_EQUAL : TokenType.EQUAL); break;
                case '<': AddToken(MatchNext('=') ? TokenType.LESS_EQUAL : TokenType.LESS); break;
                case '>': AddToken(MatchNext('=') ? TokenType.GREATER_EQUAL : TokenType.GREATER); break;

                case '/':
                    if (MatchNext('/')) // single line comments
                        while(CurrentChar() != '\n' && !AtEnd()) { EatChar(); }
                    else
                        AddToken(TokenType.SLASH);
                    break;


                case ' ':
                case '\r':
                case '\t':
                    break;//  ignore whitespace

                case '\n': currentLine++; break;

                case '"': HandleString(); break;



                default:
                    if (char.IsDigit(c))
                        HandleNumber();
                    else if (char.IsLetter(c))
                        HandleIdentifier();
                    else
                        Milt.Error(currentLine, $"Unexpected Character '{c}'");
                    break;
            }

        }

        private static void HandleIdentifier()
        {
            while (IsAlphaNumeric(CurrentChar())) { EatChar(); }

            if (!keywords.TryGetValue(CurrentLexeme(), out var type))
                type = TokenType.IDENT;

            AddToken(type);
        }

        private static void HandleNumber()
        {
            while(char.IsDigit(CurrentChar())) { EatChar(); }

            //  check for decimal
            bool isFloat = false;
            if(CurrentChar() == '.' && char.IsDigit(PeekChar()))
            {
                isFloat = true;
                EatChar(); // skip the decimal
                while(char.IsDigit(CurrentChar())) { EatChar(); }
            }

            dynamic literal = isFloat ? float.Parse(CurrentLexeme()) : int.Parse(CurrentLexeme());
            AddToken(TokenType.NUMBER, literal);
        }

        private static void HandleString()
        {
            while(CurrentChar() != '"' && !AtEnd())
            {
                if (CurrentChar() == '\n') currentLine++;
                EatChar();
            }

            if (AtEnd())
            {
                Milt.Error(currentLine, "Unterminated string.");
                return;
            }

            //  eat closing "
            EatChar();

            //  grab string literal without closing "s
            var literal = CurrentLexeme().Trim('"');
            AddToken(TokenType.STRING, literal);
        }

        private static bool AtEnd()
        {
            return currentIndex >= source.Length;
        }

        private static bool MatchNext(char expected)
        {
            if (AtEnd()) return false;
            if (source[currentIndex] != expected) return false;

            currentIndex++;
            return true;
        }

        static char EatChar()
        {
            return source[currentIndex++];
        }

        static char CurrentChar()
        {
            if (AtEnd()) return '\0';
            return source[currentIndex];
        }

        static char PeekChar()
        {
            if (currentIndex + 1 >= source.Length) return '\0';
            return source[currentIndex + 1];
        }

        static string CurrentLexeme()
        {
            return source.Substring(currentLexemeStart, currentIndex - currentLexemeStart);
        }

        static bool IsAlphaNumeric(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        static void AddToken(TokenType type) => AddToken(type, null);
        static void AddToken(TokenType type, object literal)
        {
            tokens.Add(new Token(type, CurrentLexeme(), literal, currentLine));
        }

        //  May be useful if/when converting to lex-as-needed
        //public Token PeekNextToken() 
        //{
        //    throw new NotImplementedException();
        //}

        //public Token PeekToken(int lookAhead) 
        //{
        //    throw new NotImplementedException();
        //}

        //public void EatToken()
        //{
        //    throw new NotImplementedException();
        //}

        //public Token GetLastToken()
        //{
        //    return tokens.Count > 0 ? tokens[^1] : null;
        //}
    }
}
