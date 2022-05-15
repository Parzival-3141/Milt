using System;
using System.IO;
using System.Linq;

namespace Milt
{
    public sealed class Milt
    {
        const int ERROR_BAD_ARGUMENTS = 160;
        const int ERROR_INVALID_DATA = 13;

        static bool hadError = false;

        static void Main(string[] args)
        {
            if (args.Length == 0 || args.Length > 2)
            {
                Console.WriteLine("Usage:\n milt <source-file> [-l | --log]");
                Environment.Exit(ERROR_BAD_ARGUMENTS);
            }

            byte[] bytes = null;

            try
            {
                bytes = File.ReadAllBytes(args[0]);
            }
            catch
            {
                Console.WriteLine("Invalid File.");
                Environment.Exit(ERROR_BAD_ARGUMENTS);
            }

            var tokens = Parsing.Lexer.Lex(BytesToString(bytes));

            if (args.Contains("-l") || args.Contains("--log"))
            {
                string output = "";
                foreach (var t in tokens)
                {
                    output += t.ToString() + '\n';
                    //Console.WriteLine(t);
                }

                output += ReconstructCode(tokens);

                using (var fStream = File.Create(Directory.GetParent(args[0]) + "\\" + Path.GetFileNameWithoutExtension(args[0]) + "_output.txt"))
                {
                    var buffer = StringToBytes(output);
                    fStream.Write(buffer, 0, buffer.Length);
                };
            }
            
            //  stop execution if errored out in parsing stage
            if(hadError)
            {
                Environment.Exit(ERROR_INVALID_DATA);
            }


        }

        public static string BytesToString(byte[] bytes)
        {
            string result = "";
            for (int i = 0; i < bytes.Length; i++)
            {
                result += (char)bytes[i];
            }

            return result;
        }

        public static byte[] StringToBytes(string s)
        {
            byte[] result = new byte[s.Length];
            for (int i = 0; i < s.Length; i++)
            {
                result[i] = (byte)(s[i]);
            }

            return result;
        }

        private static string ReconstructCode(System.Collections.Generic.List<Parsing.Token> tokens)
        {
            string result = "\nCode Reconstruction Based On Tokens:\n";
            for (int i = 0; i < tokens.Count; i++)
            {
                Parsing.Token t = tokens[i];
                result += t.lexeme;

                switch (t.type)
                {
                    case Parsing.TokenType.IDENT:
                    case Parsing.TokenType.FUNC: 
                    case Parsing.TokenType.RETURN: 
                        result += " "; 
                        break;

                    case Parsing.TokenType.BRACE_RIGHT:
                    case Parsing.TokenType.SEMICOLON:
                        result += "\n";
                        break;

                    case Parsing.TokenType.BRACE_LEFT:
                        result = result.Insert(result.Length - 1, "\n");
                        result += "\n";
                        break;
                }
            }

            return result;
        }

        public static void Error(int line, string message)
        {
            hadError = true;
            Console.WriteLine($"Error at line '{line}': {message}");
        }
    }
}
