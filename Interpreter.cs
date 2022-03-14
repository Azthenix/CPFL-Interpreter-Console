using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CPFL_Interpreter_Console
{
	class Interpreter
	{
		List<Token> tokenList;
		List<string> tokenLiterals;

		Dictionary<string, Type> variables;
		Dictionary<string, int> intVars;
		Dictionary<string, char> charVars;
		Dictionary<string, bool> boolVars;
		Dictionary<string, float> floatVars;

		const string symbols = "()[]*/+-%&><>=,#:\"\'";
		List<string> symbolsArray = new List<string>{"(", ")", "[", "]", "*", "/", "+", "-", "%", "&", ">", "<", "==", ">=", "<=", "=", ","};
		List<string> reserved = new List<string>{"INT", "CHAR", "BOOL", "FLOAT", "AND", "OR", "NOT",
										"VAR", "AS", "START", "STOP", "OUTPUT"};

		string[] lines;

		public Interpreter(string file)
		{
			tokenList = new List<Token>();
			tokenLiterals = new List<string>();

			lines = File.ReadAllText(file)
			.Replace("\"TRUE\"", "TRUE")
			.Replace("\"FALSE\"", "FALSE")
			.Replace("[\"]", "$DQUOTE$")
			.Split(new char[]{'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);

			for(int x = 0; x < lines.Length; x++)
			{
				string[] dqsplit = lines[x].Split('\"');

				for(int y = 1; y < dqsplit.Length; y += 2)
				{
					dqsplit[y] = dqsplit[y].Replace("[[]", "$LBRACKET$").Replace("[]]", "$BRACKET$").Replace("[#]", "$SHARP$");
				}

				lines[x] = String.Join('\"', dqsplit);
			}
		}

		public void Run()
		{
			Parse();
			foreach(Token token in tokenList)
			{
				Console.WriteLine(token.lex == Lexeme.CONSTANT ? token.literal : token.lex);
			}
			// Parse();
			// Tokenize();
			// ASTGenerator();
			// foreach(string literal in tokenLiterals)
			// {
			// 	if(literal == "NEWLINE")
			// 	{
			// 		Console.WriteLine();
			// 		continue;
			// 	}
			// 	Console.Write(literal + " ");
			// }
		}

		void Parse()
		{
			int ctr = 1;
			foreach(string line in lines)
			{

				string ln = line.Trim();

				if(ln[0] == '*')
					continue;

				StringBuilder lit = new StringBuilder();

				for(int x = 0; x < ln.Length; x++)
				{
					Console.WriteLine("" + ctr + " " + x);
					if(Char.IsLetterOrDigit(ln[x]) || ln[x] == '_')
					{
						lit.Append(ln[x]);
					}
					else
					{
						if(symbols.Contains(ln[x]))
						{
							if(lit.Length > 0)
								addToken(lit.ToString(), ctr);
							
							switch(ln[x])
							{
								case '(':
									tokenList.Add(new Token(Lexeme.LPAR, null, ctr));
									break;
								case ')':
									tokenList.Add(new Token(Lexeme.RPAR, null, ctr));
									break;
								case '*':
									tokenList.Add(new Token(Lexeme.AST, null, ctr));
									break;
								case '/':
									tokenList.Add(new Token(Lexeme.FSLASH, null, ctr));
									break;
								case '+':
									tokenList.Add(new Token(Lexeme.PLUS, null, ctr));
									break;
								case '-':
									tokenList.Add(new Token(Lexeme.MINUS, null, ctr));
									break;
								case '%':
									tokenList.Add(new Token(Lexeme.PERCENT, null, ctr));
									break;
								case '&':
									tokenList.Add(new Token(Lexeme.AMP, null, ctr));
									break;
								case '>':
									tokenLiterals.Add(lit.ToString());
									if(x < ln.Length && ln[x+1] == '=')
									{
										tokenList.Add(new Token(Lexeme.GEQUAL, null, ctr));
										tokenLiterals.Add(">=");
										x++;
									}
									else
									{
										tokenList.Add(new Token(Lexeme.GREATER, null, ctr));
										tokenLiterals.Add(">");
									}
									break;
								case '<':
									tokenLiterals.Add(lit.ToString());
									if(x < ln.Length && ln[x+1] == '=')
									{
										tokenList.Add(new Token(Lexeme.LEQUAL, null, ctr));
										tokenLiterals.Add("<=");
										x++;
									}
									else if(x < ln.Length && ln[x+1] == '>')
									{
										tokenList.Add(new Token(Lexeme.NEQUAL, null, ctr));
										tokenLiterals.Add("<>");
									}
									else
									{
										tokenList.Add(new Token(Lexeme.LESSER, null, ctr));
										tokenLiterals.Add("<");
									}
									break;
								case '=':
									tokenLiterals.Add(lit.ToString());
									if(x < ln.Length && ln[x+1] == '=')
									{
										tokenList.Add(new Token(Lexeme.EQUAL, null, ctr));
										x++;
									}
									else
									{
										tokenList.Add(new Token(Lexeme.ASSIGN, null, ctr));
									}
									break;
								case ',':
									tokenList.Add(new Token(Lexeme.COMMA, null, ctr));
									break;
								case '\"':
									lit.Clear();
									try
									{
										while(ln[++x] != '\"')
										{
											if(ln[x] == '[' || ln[x] == ']')
											{
												throw new ErrorException($"Illegal escape code '{ln[x]}' on line {ctr}.");
											}

											lit.Append(ln[x]);
										}
										tokenList.Add(new Token(Lexeme.CONSTANT, lit.ToString(), ctr));
										lit.Clear();
									}
									catch(IndexOutOfRangeException)
									{
										throw new ErrorException($"Missing '\"' on line {ctr}.");
									}
									tokenList.Add(new Token(Lexeme.LPAR, null, ctr));
									lit.Clear();
									break;
								default:
									lit.Clear();
									try
									{
										Console.WriteLine(ln[x+1]);
										if(ln[x+2] == '\'')
										{
											Console.WriteLine(ln[x+2]);

											if(ln[x+1] == '[' || ln[x+1] == ']' || ln[x+1] == '\"')
											{
												throw new ErrorException($"Illegal character '{ln[x+1]}' on line {ln[x+1]}.");
											}
											lit.Append(ln[x+1]);
											tokenList.Add(new Token(Lexeme.CONSTANT, ln[x+1] == '#' ? "\n" : lit.ToString(), ctr));

											x += 2;
										}
										else if(ln[x+4] == '\'' )
										{
											string sub = ln.Substring(x+1, 3);
											if(sub == "[[]" || sub == "[]]" || sub == "[\"]" || sub == "[#]")
											{
												tokenList.Add(new Token(Lexeme.CONSTANT, sub, ctr));
											}

											x += 4;
										}
										lit.Clear();
									}
									catch(IndexOutOfRangeException)
									{
										throw new ErrorException($"Missing ''' on line {ln[x+1]}.");
									}
									break;
							}
							
							lit.Clear();
						}
						else if(ln[x] == ' ')
						{
							if(lit.Length > 0)
							{
								addToken(lit.ToString(), ctr);
								lit.Clear();
							}
						}
						else
						{
							throw new ErrorException($"Unknown character '{ln[x]}' on line {ctr}.");
						}
					}
				}
				
				if(lit.Length > 0)
					addToken(lit.ToString(), ctr);
				
				tokenList.Add(new Token(Lexeme.NEWLINE, null, ctr));
				ctr++;
			}
		}

		void addToken(string literal, int ctr)
		{
			if(reserved.Contains(literal))
			{
				tokenList.Add(new Token(Enum.Parse<Lexeme>(literal), null, ctr));
				return;
			}
			if(Char.IsDigit(literal[0]))
			{
				if(!literal.All(x => Char.IsDigit(x) || x == '.'))
				{
					throw new ErrorException($"Illegal Identifier '{literal}' on line {ctr}.");
				}

				tokenList.Add(new Token(Lexeme.CONSTANT, literal, ctr));
				return;
			}
			if(Char.IsLetter(literal[0]) || literal[0] == '_')
			{
				if(!literal.All(x => Char.IsLetterOrDigit(x) || x == '_'))
				{
					throw new ErrorException($"Illegal Identifier '{literal}' on line {ctr}.");
				}

				tokenList.Add(new Token(Lexeme.CONSTANT, literal, ctr));
				return;
			}
		}

		void Tokenize()
		{
			int ctr = 1;
			foreach(string lit in tokenLiterals)
			{
				if(reserved.Contains(lit))
				{
					tokenList.Add(new Token(Enum.Parse<Lexeme>(lit), null, ctr));
				}
				else if(symbols.Contains(lit))
				{
					switch(lit)
					{
						case "(":
							tokenList.Add(new Token(Lexeme.LPAR, null, ctr));
							break;
						case ")":
							tokenList.Add(new Token(Lexeme.RPAR, null, ctr));
							break;
						case "[":
							tokenList.Add(new Token(Lexeme.LBRACK, null, ctr));
							break;
						case "]":
							tokenList.Add(new Token(Lexeme.RBRACK, null, ctr));
							break;
						case "*":
							tokenList.Add(new Token(Lexeme.AST, null, ctr));
							break;
						case "/":
							tokenList.Add(new Token(Lexeme.FSLASH, null, ctr));
							break;
						case "+":
							tokenList.Add(new Token(Lexeme.PLUS, null, ctr));
							break;
						case "-":
							tokenList.Add(new Token(Lexeme.MINUS, null, ctr));
							break;
						case "%":
							tokenList.Add(new Token(Lexeme.PERCENT, null, ctr));
							break;
						case "&":
							tokenList.Add(new Token(Lexeme.AMP, null, ctr));
							break;
						case ">":
							tokenList.Add(new Token(Lexeme.GREATER, null, ctr));
							break;
						case "<":
							tokenList.Add(new Token(Lexeme.LESSER, null, ctr));
							break;
						case "=":
							tokenList.Add(new Token(Lexeme.LPAR, null, ctr));
							break;
						case ">=":
							tokenList.Add(new Token(Lexeme.LPAR, null, ctr));
							break;
						case "<=":
							tokenList.Add(new Token(Lexeme.LPAR, null, ctr));
							break;
						case "==":
							tokenList.Add(new Token(Lexeme.LPAR, null, ctr));
							break;
						case ",":
							tokenList.Add(new Token(Lexeme.LPAR, null, ctr));
							break;
						case "#":
							tokenList.Add(new Token(Lexeme.LPAR, null, ctr));
							break;
						case ":":
							tokenList.Add(new Token(Lexeme.LPAR, null, ctr));
							break;
						case "\"":
							tokenList.Add(new Token(Lexeme.LPAR, null, ctr));
							break;
						default:
							tokenList.Add(new Token(Lexeme.LPAR, null, ctr));
							break;
					}
				}
			}
		}
		void ASTGenerator()
		{

		}

		public bool CheckChars(string str, string cmp)
		{
			foreach(char c in str)
			{
				if(cmp.Contains(c))
					return true;
			}

			return false;
		}
	}
}