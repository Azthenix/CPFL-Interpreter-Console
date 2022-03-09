using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CPFL_Interpreter_Console
{
	class Interpreter
	{
		List<Token> tokens;

		Dictionary<string, Type> variables;
		Dictionary<string, int> intVars;
		Dictionary<string, char> charVars;
		Dictionary<string, bool> boolVars;
		Dictionary<string, float> floatVars;

		const string symbols = "()*/%+-><=,";
		List<string> reserved = new List<string>{"INT", "CHAR", "BOOL", "FLOAT",
										"VAR", "AS", "START", "STOP", "OUTPUT"};

		string[] lines;

		public Interpreter(string file)
		{
			lines = File.ReadAllText(file).Split('\n', StringSplitOptions.RemoveEmptyEntries);
		}

		public void Run()
		{
			Tokenize();
			foreach(Token token in tokens)
			{
				Console.WriteLine(token);
			}
		}

		void Tokenize()
		{
			StringBuilder sb;

			for(int line = 0; line < lines.Length; line++)
			{
				sb = new StringBuilder();

				string ln = lines[line].Trim();

				for(int ctr = 0; ctr < ln.Length; ctr++)
				{
					char c = ln[ctr];

					if(c == '\n')
					{
						string literal = sb.ToString();

						//Reserved
						if(reserved.Contains(literal))
						{
							tokens.Add(new Token(TokenType.RESERVED, literal, ctr));
						}
						//Identifier
						else if(Char.IsLetter(literal[0]) || literal[0] == '_')
						{
							tokens.Add(new Token(TokenType.IDENTIFIER, literal, ctr));
						}

						tokens.Add(new Token(TokenType.NEWLINE, null, ctr));
					}
					if(c == ' ')
					{
						if(ln[ctr-1] != ' ' && sb.Length != 0)
						{
							string literal = sb.ToString();

							//Reserved
							if(reserved.Contains(literal))
							{
								tokens.Add(new Token(TokenType.RESERVED, literal, ctr));
							}
							//Identifier
							else if(Char.IsLetter(literal[0]) || literal[0] == '_')
							{
								tokens.Add(new Token(TokenType.IDENTIFIER, literal, ctr));
							}

							sb = new StringBuilder();
						}
						else
						{
							continue;
						}
					}

					if(c == '\'')
					{
						if(ln[ctr+2] == '\'')
						{
							sb.Append($"\'{ln[ctr+1]}\'");

							tokens.Add(new Token(TokenType.CONSTANT, sb.ToString(), ctr));
							
							sb = new StringBuilder();
						}
						else
						{
							throw new ErrorException($"Error on line {ctr+1}");
						}
					}

					if(c == '\"')
					{
						if(sb.Length > 0)
						{
							string literal = sb.ToString();

							//Reserved
							if(reserved.Contains(literal))
							{
								tokens.Add(new Token(TokenType.RESERVED, literal, ctr));
							}
							//Identifier
							else if(Char.IsLetter(literal[0]) || literal[0] == '_')
							{
								tokens.Add(new Token(TokenType.IDENTIFIER, literal, ctr));
							}
							sb = new StringBuilder();
						}
						
						while(c != '\n')
						{

						}
					}

					if(symbols.Contains(c))
					{
						if(c == '<' || c == '>' || c == '=')
						{
							if(ln[ctr+1] == '=')
							{
								sb.Append(c);
								sb.Append('=');
								tokens.Add(new Token(TokenType.SYMBOL, sb.ToString(), ctr));
							}
							else
							{
								sb.Append(c);
								tokens.Add(new Token(TokenType.SYMBOL, sb.ToString(), ctr));
							}
							
							sb = new StringBuilder();
						}
					}
				}

				// if(ln.StartsWith("VAR "))
				// {
				// 	ln = ln.Replace("VAR ", "");
				// 	switch(ln.Substring(ln.IndexOf(" AS")).Trim())
				// 	{
				// 		case "AS INT":
				// 			Console.WriteLine("int");
				// 			break;
				// 		case "AS CHAR":
				// 			Console.WriteLine("char");
				// 			break;
				// 		case "AS BOOL":
				// 			Console.WriteLine("bool");
				// 			break;
				// 		case "AS FLOAT":
				// 			Console.WriteLine("float");
				// 			break;
				// 		default:
				// 			Console.WriteLine($"No such data type as '{ln.Split(' ')[^1]}'");
				// 			throw new TypeAccessException();
				// 			break;
				// 	}

				// 	ln = ln.Remove(ln.IndexOf(" AS"));
				// 	Console.WriteLine(ln);
				// }

			}
		}

		int accessInt(string name)
		{
			if(intVars == null)
			{
				intVars = new Dictionary<string, int>();
			}

			return 0;
		}
	}
}