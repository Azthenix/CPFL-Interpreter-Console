// using System;
// using System.IO;
// using System.Text;
// using System.Collections.Generic;
// using System.Text.RegularExpressions;

// namespace CPFL_Interpreter_Console
// {
// 	class Interpreter
// 	{
// 		List<Token> tokenList;
// 		List<string> tokenLiterals;

// 		Dictionary<string, Type> variables;
// 		Dictionary<string, int> intVars;
// 		Dictionary<string, char> charVars;
// 		Dictionary<string, bool> boolVars;
// 		Dictionary<string, float> floatVars;

// 		const string symbols = "()[]*/+-%&><>=,#:\"\'";
// 		List<string> symbolsArray = new List<string>{"(", ")", "[", "]", "*", "/", "+", "-", "%", "&", ">", "<", "==", ">=", "<=", "=", ","};
// 		List<string> reserved = new List<string>{"INT", "CHAR", "BOOL", "FLOAT", "AND", "OR", "NOT",
// 										"VAR", "AS", "START", "STOP", "OUTPUT"};

// 		string[] lines;

// 		public Interpreter(string file)
// 		{
// 			tokenList = new List<Token>();
// 			tokenLiterals = new List<string>();

// 			lines = File.ReadAllText(file).Replace("\"TRUE\"", "TRUE").Replace("\"FALSE\"", "FALSE").Split(new char[]{'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);
// 		}

// 		public void Run()
// 		{
// 			Parse();
// 			Tokenize();
// 			ASTGenerator();
// 			foreach(string literal in tokenLiterals)
// 			{
// 				if(literal == "NEWLINE")
// 				{
// 					Console.WriteLine();
// 					continue;
// 				}
// 				Console.Write(literal + " ");
// 			}
// 		}

// 		void Parse()
// 		{
// 			int ctr = 1;
// 			bool inquotes = false;
// 			bool inbracks = false;
// 			foreach(string line in lines)
// 			{

// 				string ln = line.Trim();

// 				if(ln[0] == '*')
// 					continue;

// 				StringBuilder lit = new StringBuilder();

// 				for(int x = 0; x < ln.Length; x++)
// 				{
// 					if(Char.IsLetterOrDigit(ln[x]) || ln[x] == '_')
// 					{
// 						lit.Append(ln[x]);
// 					}
// 					else
// 					{
// 						if(!inquotes)
// 						{
// 							if(symbols.Contains(ln[x]))
// 							{
// 								switch(ln[x])
// 								{
// 									case '(':
// 										tokenLiterals.Add(lit.ToString());
// 										tokenLiterals.Add("(");
// 										break;
// 									case ')':
// 										tokenLiterals.Add(lit.ToString());
// 										tokenLiterals.Add(")");
// 										break;
// 									case '[':
// 										tokenLiterals.Add(lit.ToString());
// 										tokenLiterals.Add("[");
// 										break;
// 									case ']':
// 										tokenLiterals.Add(lit.ToString());
// 										tokenLiterals.Add("]");
// 										break;
// 									case '*':
// 										tokenLiterals.Add(lit.ToString());
// 										tokenLiterals.Add("*");
// 										break;
// 									case '/':
// 										tokenLiterals.Add(lit.ToString());
// 										tokenLiterals.Add("/");
// 										break;
// 									case '+':
// 										tokenLiterals.Add(lit.ToString());
// 										tokenLiterals.Add("+");
// 										break;
// 									case '-':
// 										tokenLiterals.Add(lit.ToString());
// 										tokenLiterals.Add("-");
// 										break;
// 									case '%':
// 										tokenLiterals.Add(lit.ToString());
// 										tokenLiterals.Add("%");
// 										break;
// 									case '&':
// 										tokenLiterals.Add(lit.ToString());
// 										tokenLiterals.Add("&");
// 										break;
// 									case '>':
// 										tokenLiterals.Add(lit.ToString());
// 										if(x < ln.Length && ln[x+1] == '=')
// 										{
// 											tokenLiterals.Add(">=");
// 											x++;
// 										}
// 										else
// 										{
// 											tokenLiterals.Add(">");
// 										}
// 										break;
// 									case '<':
// 										tokenLiterals.Add(lit.ToString());
// 										if(x < ln.Length && ln[x+1] == '=')
// 										{
// 											tokenLiterals.Add("<=");
// 											x++;
// 										}
// 										else if(x < ln.Length && ln[x+1] == '>')
// 										{
// 											tokenLiterals.Add("<>");
// 										}
// 										else
// 										{
// 											tokenLiterals.Add("<");
// 										}
// 										break;
// 									case '=':
// 										tokenLiterals.Add(lit.ToString());
// 										if(x < ln.Length && ln[x+1] == '=')
// 										{
// 											tokenLiterals.Add("==");
// 											x++;
// 										}
// 										else
// 										{
// 											tokenLiterals.Add("=");
// 										}
// 										break;
// 									case ',':
// 										tokenLiterals.Add(lit.ToString());
// 										tokenLiterals.Add(",");
// 										break;
// 									case '#':
// 										tokenLiterals.Add(lit.ToString());
// 										tokenLiterals.Add("#");
// 										break;
// 									case ':':
// 										tokenLiterals.Add(lit.ToString());
// 										tokenLiterals.Add(":");
// 										break;
// 									case '\"':
// 										tokenLiterals.Add(lit.ToString());
// 										tokenLiterals.Add("\"");
// 										inquotes = !inquotes;
// 										lit.Clear();
// 										break;
// 									default:
// 										tokenLiterals.Add(lit.ToString());
// 										tokenLiterals.Add("\'");
// 										break;
// 								}
								
// 								lit.Clear();
// 							}
// 							if(ln[x-1] != ' ')
// 							{
// 								tokenLiterals.Add(lit.ToString());
// 								lit.Clear();
// 							}
// 						}
// 						else
// 						{
// 							if(!inbracks)
// 							{
// 								lit.Append(ln[x]);
// 							}
// 							else
// 							{
// 								switch(ln[x])
// 								{
// 									case '[':
// 										tokenLiterals.Add(lit.ToString());
// 										tokenLiterals.Add("[");
// 										inbracks = true;
// 										lit.Clear();
// 										break;
// 									case ']':
// 										tokenLiterals.Add(lit.ToString());
// 										tokenLiterals.Add("]");
// 										lit.Clear();
// 										break;
// 									case '"':
// 										tokenLiterals.Add(lit.ToString());
// 										tokenLiterals.Add("\"");
// 										inquotes = false;
// 										lit.Clear();
// 										break;
// 									default:
// 										lit.Append(ln[x]);
// 										break;
// 								}
// 							}
// 						}
// 					}
// 				}
				
// 				tokenLiterals.Add(lit.ToString());
// 				tokenLiterals.Add("NEWLINE");
// 				ctr++;
// 			}
// 		}

// 		void Tokenize()
// 		{
// 			int ctr = 1;
// 			bool inquotes = false;
// 			foreach(string lit in tokenLiterals)
// 			{
// 				if(reserved.Contains(lit))
// 				{
// 					tokenList.Add(new Token(Enum.Parse<Lexeme>(lit), null, ctr));
// 				}
// 				else if(!inquotes)
// 				{
// 					if(symbols.Contains(lit))
// 					{
// 						switch(lit)
// 						{
// 							case "(":
// 								tokenList.Add(new Token(Lexeme.LPAR, null, ctr));
// 								break;
// 							case ")":
// 								tokenList.Add(new Token(Lexeme.RPAR, null, ctr));
// 								break;
// 							case "[":
// 								tokenList.Add(new Token(Lexeme.LBRACK, null, ctr));
// 								break;
// 							case "]":
// 								tokenList.Add(new Token(Lexeme.RBRACK, null, ctr));
// 								break;
// 							case "*":
// 								tokenList.Add(new Token(Lexeme.AST, null, ctr));
// 								break;
// 							case "/":
// 								tokenList.Add(new Token(Lexeme.FSLASH, null, ctr));
// 								break;
// 							case "+":
// 								tokenList.Add(new Token(Lexeme.PLUS, null, ctr));
// 								break;
// 							case "-":
// 								tokenList.Add(new Token(Lexeme.MINUS, null, ctr));
// 								break;
// 							case "%":
// 								tokenList.Add(new Token(Lexeme.PERCENT, null, ctr));
// 								break;
// 							case "&":
// 								tokenList.Add(new Token(Lexeme.AMP, null, ctr));
// 								break;
// 							case ">":
// 								tokenList.Add(new Token(Lexeme.GREATER, null, ctr));
// 								break;
// 							case "<":
// 								tokenList.Add(new Token(Lexeme.LESSER, null, ctr));
// 								break;
// 							case "=":
// 								tokenList.Add(new Token(Lexeme.ASSIGN, null, ctr));
// 								break;
// 							case ">=":
// 								tokenList.Add(new Token(Lexeme.GEQUAL, null, ctr));
// 								break;
// 							case "<=":
// 								tokenList.Add(new Token(Lexeme.LEQUAL, null, ctr));
// 								break;
// 							case "==":
// 								tokenList.Add(new Token(Lexeme.EQUAL, null, ctr));
// 								break;
// 							case "<>":
// 								tokenList.Add(new Token(Lexeme.NEQUAL, null, ctr));
// 								break;
// 							case ",":
// 								tokenList.Add(new Token(Lexeme.COMMA, null, ctr));
// 								break;
// 							case "#":
// 								tokenList.Add(new Token(Lexeme.SHARP, null, ctr));
// 								break;
// 							case ":":
// 								tokenList.Add(new Token(Lexeme.COLON, null, ctr));
// 								break;
// 							case "\"":
// 								tokenList.Add(new Token(Lexeme.DQUOTE, null, ctr));
// 								inquotes = true;
// 								break;
// 							default:
// 								tokenList.Add(new Token(Lexeme.QUOTE, null, ctr));
// 								break;
// 						}
// 					}
// 				}
// 				else if(lit == "NEWLINE")
// 				{
// 					tokenList.Add(new Token(Lexeme.NEWLINE, null, ctr++));
// 				}
// 			}
// 		}
// 		void ASTGenerator()
// 		{

// 		}

// 		public bool CheckChars(string str, string cmp)
// 		{
// 			foreach(char c in str)
// 			{
// 				if(cmp.Contains(c))
// 					return true;
// 			}

// 			return false;
// 		}
// 	}
// }