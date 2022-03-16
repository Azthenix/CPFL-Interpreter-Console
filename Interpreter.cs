using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Data;

namespace CPFL_Interpreter_Console
{
	class Interpreter
	{
		List<Token> tokenList;
		List<string> tokenLiterals;

		Node root;

		int[,] DeclareDFA = new int[7,7]{
			{1, -1, -1, -1, -1, -1, -1},
			{-1, 2, -1, -1, -1, -1, -1},
			{-1, -1, 3, 1, -1, 5, -1},
			{-1, 4, -1, -1, 4, -1, -1},
			{-1, -1, -1, 1, -1, 5, -1},
			{-1, -1, -1, -1, -1, -1, 6},
			{-1, -1, -1, -1, -1, -1, -1}
		};

		int[,] outputDFA = new int[4,4]{
			{1, -1, -1, -1},
			{-1, 2, -1, -1},
			{-1, -1, 3, -1},
			{-1, -1, -1, 2}
		};

		enum bType
		{
			INT,
			FLOAT,
			CHAR,
			BOOL
		};

		Dictionary<string, bType> variables;
		Dictionary<string, int> intVars;
		Dictionary<string, char> charVars;
		Dictionary<string, bool> boolVars;
		Dictionary<string, float> floatVars;

		const string symbols = "()[]*/+-%&><>=,#:\"\'";
		List<string> symbolsArray = new List<string>{"(", ")", "[", "]", "*", "/", "+", "-", "%", "&", ">", "<", "==", ">=", "<=", "=", ","};
		List<string> reserved = new List<string>{"INT", "CHAR", "BOOL", "FLOAT", "AND", "OR", "NOT", "WHILE", "TRUE", "FALSE",
										"VAR", "AS", "START", "STOP", "OUTPUT"};

		string[] lines;

		public Interpreter(string file)
		{
			variables = new Dictionary<string, bType>();
			intVars = new Dictionary<string, int>();
			charVars = new Dictionary<string, char>();
			boolVars = new Dictionary<string, bool>();
			floatVars = new Dictionary<string, float>();

			tokenList = new List<Token>();
			tokenLiterals = new List<string>();
			root = new Node(null, null);

			lines = File.ReadAllText(file)
			.Replace("\"TRUE\"", "TRUE")
			.Replace("\"FALSE\"", "FALSE")
			.Replace("[\"]", "$DQUOTE$")
			.Replace("\r", "")
			.Split('\n');

			for(int x = 0; x < lines.Length; x++)
			{
				string[] dqsplit = lines[x].Split('\"');

				for(int y = 1; y < dqsplit.Length; y += 2)
				{
					dqsplit[y] = dqsplit[y].Replace("[[]", "$LBRACKET$").Replace("[]]", "$RBRACKET$").Replace("[#]", "$SHARP$");
				}

				lines[x] = String.Join('\"', dqsplit);
			}
		}

		public void Run()
		{
			Parse();
			Interpret();
			//ASTGenerator();
			// foreach(Token token in tokenList)
			// {
			// 	Console.WriteLine(token.lex == Lexeme.CONSTANT ? token.literal : token.lex);
			// }

			//TraverseAST(root.right);
		}

		void Parse()
		{
			int ctr = 1;
			foreach(string line in lines)
			{
				string ln = line.Trim();

				if(ln.Length == 0 || ln[0] == '*')
				{
					ctr++;
					continue;
				}

				StringBuilder lit = new StringBuilder();

				for(int x = 0; x < ln.Length; x++)
				{
					if(Char.IsLetterOrDigit(ln[x]) || ln[x] == '_' || ln[x] == '.')
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
								case ':':
									tokenList.Add(new Token(Lexeme.COLON, null, ctr));
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
										tokenList.Add(new Token(Lexeme.STRING, lit.ToString(), ctr));
										lit.Clear();
									}
									catch(IndexOutOfRangeException)
									{
										throw new ErrorException($"Missing '\"' on line {ctr}.");
									}
									lit.Clear();
									break;
								default:
									lit.Clear();
									try
									{
										if(ln[x+2] == '\'')
										{
											if(ln[x+1] == '[' || ln[x+1] == ']' || ln[x+1] == '\"')
											{
												throw new ErrorException($"Illegal character '{ln[x+1]}' on line {ln[x+1]}.");
											}
											lit.Append(ln[x+1]);
											tokenList.Add(new Token(Lexeme.CHARACTER, ln[x+1] == '#' ? "\n" : lit.ToString(), ctr));

											x += 2;
										}
										else if(ln[x+4] == '\'' )
										{
											string sub = ln.Substring(x+1, 3);
											if(sub == "[[]" || sub == "[]]" || sub == "[\"]" || sub == "[#]")
											{
												tokenList.Add(new Token(Lexeme.CHARACTER, sub, ctr));
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
				ctr += 1;
			}
		}

		void addToken(string literal, int ctr)
		{
			if(reserved.Contains(literal))
			{
				if(literal == "TRUE" || literal == "FALSE")
				{
					tokenList.Add(new Token(Lexeme.BOOLEAN, literal, ctr));
				}
				else
				{
					tokenList.Add(new Token(Enum.Parse<Lexeme>(literal), null, ctr));
				}
				return;
			}
			if(Char.IsDigit(literal[0]))
			{
				if(!literal.All(x => Char.IsDigit(x) || x == '.'))
				{
					throw new ErrorException($"Illegal Identifier '{literal}' on line {ctr}.");
				}

				tokenList.Add(new Token(Lexeme.NUMBER, literal, ctr));
				return;
			}
			if(Char.IsLetter(literal[0]) || literal[0] == '_')
			{
				if(!literal.All(x => Char.IsLetterOrDigit(x) || x == '_'))
				{
					throw new ErrorException($"Illegal Identifier '{literal}' on line {ctr}.");
				}

				tokenList.Add(new Token(Lexeme.IDENTIFIER, literal, ctr));
				return;
			}

			throw new ErrorException($"Illegal Identifier '{literal}' on line {ctr}.");
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
			List<Token> tks = new List<Token>();
			tks.AddRange(tokenList);
			Node currNode = root;

			while(tks.Count > 0)
			{
				switch(tks[0].lex)
				{
					case Lexeme.VAR:

						while(tks[0].lex != Lexeme.NEWLINE)
						{
							switch(tks[0].lex)
							{
								case Lexeme.VAR:
								case Lexeme.AS:
								case Lexeme.STRING:
								case Lexeme.NUMBER:
								case Lexeme.CHARACTER:
								case Lexeme.BOOLEAN:
									currNode.right = new Node(currNode, tks[0]);
									currNode = currNode.right;
									break;
								case Lexeme.ASSIGN:
								case Lexeme.IDENTIFIER:
									currNode.left = new Node(currNode, tks[0]);
									currNode = currNode.left;
									break;
								case Lexeme.COMMA:
									while(currNode.token.lex != Lexeme.IDENTIFIER)
									{
										currNode = currNode.parent;
									}
									currNode.right = new Node(currNode, tks[0]);
									currNode = currNode.right;
									break;
								case Lexeme.INT:
								case Lexeme.FLOAT:
								case Lexeme.CHAR:
								case Lexeme.BOOL:
									while(currNode.token.lex != Lexeme.VAR)
									{
										if(currNode is null)
											throw new ErrorException($"Illegal '{tks[0].lex}' on line {tks[0].line}.");
										
										currNode = currNode.parent;
									}
									if(currNode.right is not null)
										throw new ErrorException($"Illegal '{tks[0].lex}' on line {tks[0].line}.");
									
									currNode.right = new Node(currNode, tks[0]);
									currNode = currNode.right;
									break;
								case Lexeme.NEWLINE:
									currNode.left = new Node(currNode, tks[0]);
									currNode = currNode.left;
									break;
								default:
									throw new ErrorException($"Illegal '{tks[0].lex}' on line {tks[0].line}.");
							}

							tks.RemoveAt(0);
						}

						//Console.WriteLine(sequences[^1].right.displayNode());
						break;
					case Lexeme.START:
						currNode.right = new Node(currNode, tks[0]);
						currNode = currNode.right;
						tks.RemoveAt(0);

						while(tks.Count > 0)
						{
							switch(tks[0].lex)
							{
								case Lexeme.START:
								case Lexeme.WHILE:
								case Lexeme.IF:
								case Lexeme.STRING:
								case Lexeme.NUMBER:
								case Lexeme.AMP:
								case Lexeme.CHARACTER:
								case Lexeme.OUTPUT:
								case Lexeme.INPUT:
									currNode.left = new Node(currNode, tks[0]);
									currNode = currNode.left;
									break;
								case Lexeme.ELSE:
									while(currNode.token.lex != Lexeme.IF)
									{
										currNode = currNode.parent;
									}
									currNode.right = new Node(currNode, tks[0]);
									currNode = currNode.right;
									break;
								case Lexeme.STOP:
									while(currNode.token.lex != Lexeme.START || currNode.right is not null)
									{
										currNode = currNode.parent;
									}
									currNode.right = new Node(currNode, tks[0]);
									currNode = currNode.right;
									break;
								case Lexeme.ASSIGN:
								case Lexeme.IDENTIFIER:
								case Lexeme.LPAR:
								case Lexeme.RPAR:
								case Lexeme.AST:
								case Lexeme.FSLASH:
								case Lexeme.PLUS:
								case Lexeme.MINUS:
								case Lexeme.PERCENT:
								case Lexeme.GREATER:
								case Lexeme.LESSER:
								case Lexeme.GEQUAL:
								case Lexeme.LEQUAL:
								case Lexeme.NEQUAL:
									currNode.left = new Node(currNode, tks[0]);
									currNode = currNode.left;
									break;
								case Lexeme.NEWLINE:
									currNode.left = new Node(currNode, tks[0]);
									currNode = currNode.left;
									break;
								default:
									Console.WriteLine($"{tks[0].lex} {tks[1].lex} {tks[2].lex}.");
									throw new ErrorException($"Illegal '{tks[0].lex}' on line {tks[0].line}.");
							}

							tks.RemoveAt(0);
						}

						break;
					case Lexeme.NEWLINE:
						tks.RemoveAt(0);
						break;
					default:
						throw new ErrorException($"Illegal {tks[0].lex} on line {tks[0].line}.");
				}
			}
		}

		void Interpret()
		{
			List<Token> tks = new List<Token>(tokenList);

			for(int x = 0; x < tks.Count; x++)
			{
				switch(tks[x].lex)
				{
					case Lexeme.VAR:
						checkDeclare(x);
						Declare(x, ref x);
						break;
					case Lexeme.START:
						executeBody(x, ref x);
						break;
					case Lexeme.NEWLINE:
						break;
					default:
						throw new ErrorException($"Illegal '{tks[x].lex}' on line {tks[x].line}.");
				}
			}
		}

		void executeBody(int index, ref int y)
		{
			int x = index;
			for(x = x+1; x < tokenList.Count; x++)
			{
				switch(tokenList[x].lex)
				{
					case Lexeme.IDENTIFIER:
						if(tokenList[x+1].lex != Lexeme.ASSIGN)
						{
							throw new ErrorException($"Illegal '{tokenList[x].lex}' on line {tokenList[x].line}.");
						}

						switch(variables[tokenList[x].literal])
						{
							case bType.INT:
							case bType.FLOAT:
								assignToNum(x, ref x);
								break;
							case bType.CHAR:
								break;
							case bType.BOOL:
								break;
						}
						break;
					case Lexeme.START:
						executeBody(x, ref x);
						break;
					case Lexeme.STOP:
						y = x;
						return;
					case Lexeme.OUTPUT:
						checkOutput(x);
						Outut(x, ref x);
						break;
					case Lexeme.NEWLINE:
						break;
					default:
						throw new ErrorException($"Illegal '{tokenList[x].lex}' on line {tokenList[x].line}.");
				}
			}
		}

		void Declare(int index, ref int y)
		{
			int x = index;
			List<string> names = new List<string>();
			bType t = bType.INT;

			while(tokenList[x].lex != Lexeme.NEWLINE)
			{
				switch(tokenList[x].lex)
				{
					case Lexeme.INT:
						t = bType.INT;
						break;
					case Lexeme.FLOAT:
						t = bType.FLOAT;
						break;
					case Lexeme.CHAR:
						t = bType.CHAR;
						break;
					case Lexeme.BOOL:
						t = bType.BOOL;
						break;
				}

				x++;
			}

			y = x;

			while(x > index)
			{
				if(tokenList[x].lex == Lexeme.IDENTIFIER)
				{
					string name = tokenList[x].literal;
					
					if(variables.ContainsKey(name))
					{
						throw new ErrorException($"Variable '{name}' has already been declared previously.");
					}

					variables[name] = t;
				}
				else if(tokenList[x].lex == Lexeme.ASSIGN)
				{
					assign(tokenList[x-1].literal, tokenList[x+1], t);
					x--;
				}
				x--;
			}
		}

		float assignToNum(int index, ref int y)
		{
			int x = index;
			float res;

			x += 2;

			if(tokenList[x+1].lex == Lexeme.ASSIGN)
			{
				if(tokenList[x].lex == Lexeme.IDENTIFIER)
				{
					res = assignToNum(x,  ref x);
					
					switch(variables[tokenList[index].literal])
					{
						case bType.INT:
							intVars[tokenList[index].literal] = Convert.ToInt32(res);
							break;
						case bType.FLOAT:
							floatVars[tokenList[index].literal] = res;
							break;
						default:
							throw new ErrorException($"Illegal IDENTIFIER '{tokenList[x].literal}' on line {tokenList[x].line}.");
					}

					y = x;

					return res;
				}
			}

			StringBuilder sb = new StringBuilder();

			while(tokenList[x].lex != Lexeme.NEWLINE)
			{
				switch(tokenList[x].lex)
				{
					case Lexeme.IDENTIFIER:
						try
						{
							switch(variables[tokenList[x].literal])
							{
								case bType.INT:
									sb.Append(intVars[tokenList[x].literal]);
									break;
								case bType.FLOAT:
									sb.Append(floatVars[tokenList[x].literal]);
									break;
								default:
									throw new ErrorException($"Illegal IDENTIFIER '{tokenList[x].literal}' on line {tokenList[x].line}.");
							}
						}
						catch(KeyNotFoundException)
						{
							throw new ErrorException($"Use of unassigned variable '{tokenList[x].literal}' on line {tokenList[x].line}.");
						}
						break;
					case Lexeme.NUMBER:
						sb.Append(tokenList[x].literal);
						break;
					case Lexeme.LPAR:
						sb.Append('(');
						break;
					case Lexeme.RPAR:
						sb.Append(')');
						break;
					case Lexeme.AST:
						sb.Append('*');
						break;
					case Lexeme.FSLASH:
						sb.Append('/');
						break;
					case Lexeme.PLUS:
						sb.Append('+');
						break;
					case Lexeme.MINUS:
						sb.Append('-');
						break;
					case Lexeme.PERCENT:
						sb.Append('%');
						break;
					default:
						throw new ErrorException($"Illegal '{tokenList[x].lex}' on line {tokenList[x].line}.");
				}
				x++;
			}

			y = x;

			DataTable dt = new DataTable();
			
			res = Convert.ToSingle(dt.Compute(sb.ToString(), ""));

			switch(variables[tokenList[index].literal])
			{
				case bType.INT:
					intVars[tokenList[index].literal] = Convert.ToInt32(dt.Compute(sb.ToString(), ""));
					break;
				case bType.FLOAT:
					floatVars[tokenList[index].literal] = Convert.ToSingle(dt.Compute(sb.ToString(), ""));
					break;
			}

			return res;
		}

		char assignToChar(int index, ref int y)
		{
			int x = index;
			char res;

			x += 2;

			if(tokenList[x+1].lex == Lexeme.ASSIGN)
			{
				if(tokenList[x].lex == Lexeme.IDENTIFIER)
				{
					res = assignToChar(x,  ref x);
					
					switch(variables[tokenList[index].literal])
					{
						case bType.INT:
							intVars[tokenList[index].literal] = Convert.ToInt32(res);
							break;
						case bType.FLOAT:
							floatVars[tokenList[index].literal] = res;
							break;
					}

					y = x;

					return res;
				}
			}

			StringBuilder sb = new StringBuilder();

			while(tokenList[x].lex != Lexeme.NEWLINE)
			{
				switch(tokenList[x].lex)
				{
					case Lexeme.IDENTIFIER:
						try
						{
							switch(variables[tokenList[x].literal])
							{
								case bType.INT:
									sb.Append(intVars[tokenList[x].literal]);
									break;
								case bType.FLOAT:
									sb.Append(floatVars[tokenList[x].literal]);
									break;
								default:
									throw new ErrorException($"Illegal IDENTIFIER '{tokenList[x].literal}' on line {tokenList[x].line}.");
							}
						}
						catch(KeyNotFoundException)
						{
							throw new ErrorException($"Use of unassigned variable '{tokenList[x].literal}' on line {tokenList[x].line}.");
						}
						break;
					case Lexeme.NUMBER:
						sb.Append(tokenList[x].literal);
						break;
					case Lexeme.LPAR:
						sb.Append('(');
						break;
					case Lexeme.RPAR:
						sb.Append(')');
						break;
					case Lexeme.AST:
						sb.Append('*');
						break;
					case Lexeme.FSLASH:
						sb.Append('/');
						break;
					case Lexeme.PLUS:
						sb.Append('+');
						break;
					case Lexeme.MINUS:
						sb.Append('-');
						break;
					case Lexeme.PERCENT:
						sb.Append('%');
						break;
					default:
						throw new ErrorException($"Illegal '{tokenList[x].lex}' on line {tokenList[x].line}.");
				}
				x++;
			}

			y = x;

			// DataTable dt = new DataTable();
			
			// res = Convert.ToSingle(dt.Compute(sb.ToString(), ""));

			// switch(variables[tokenList[index].literal])
			// {
			// 	case bType.INT:
			// 		intVars[tokenList[index].literal] = Convert.ToInt32(dt.Compute(sb.ToString(), ""));
			// 		break;
			// 	case bType.FLOAT:
			// 		floatVars[tokenList[index].literal] = Convert.ToSingle(dt.Compute(sb.ToString(), ""));
			// 		break;
			// }

			return 'b';
		}

		void Outut(int index, ref int y)
		{
			int x = index + 2;

			StringBuilder sb = new StringBuilder();

			while(tokenList[x].lex != Lexeme.NEWLINE)
			{
				switch(tokenList[x].lex)
				{
					case Lexeme.STRING:
						sb.Append(tokenList[x].literal.Replace("$DQUOTE$", "\"").Replace("$LBRACKET$", "[").Replace("$RBRACKET$", "]").Replace("#", "\n").Replace("$SHARP$", "#"));
						break;
					case Lexeme.NUMBER:
					case Lexeme.CHARACTER:
					case Lexeme.BOOLEAN:
						sb.Append(tokenList[x].literal);
						break;
					case Lexeme.IDENTIFIER:
						switch(variables[tokenList[x].literal])
						{
							case bType.INT:
								sb.Append(intVars[tokenList[x].literal]);
								break;
							case bType.FLOAT:
								sb.Append(floatVars[tokenList[x].literal]);
								break;
							case bType.CHAR:
								sb.Append(charVars[tokenList[x].literal]);
								break;
							case bType.BOOL:
								sb.Append(boolVars[tokenList[x].literal] ? "TRUE" : "FALSE");
								break;
						}
						break;
					default:
						break;
				}

				x++;
			}

			y = x;

			Console.Write(sb.ToString());
		}

		void checkOutput(int index)
		{
			int state = 0;
			int x = index;

			while(tokenList[x].lex != Lexeme.NEWLINE)
			{
				switch(tokenList[x].lex)
				{
					case Lexeme.OUTPUT:
						state = outputDFA[state, 0];
						break;
					case Lexeme.COLON:
						state = outputDFA[state, 1];
						break;
					case Lexeme.STRING:
					case Lexeme.NUMBER:
					case Lexeme.CHARACTER:
					case Lexeme.BOOLEAN:
					case Lexeme.IDENTIFIER:
						state = outputDFA[state, 2];
						break;
					case Lexeme.AMP:
						state = outputDFA[state, 3];
						break;
					default:
						state = -1;
						break;
				}

				if(state == -1)
				{
					throw new ErrorException($"Illegal {tokenList[x].lex} on line {tokenList[x].line}.");
				}

				x++;
			}
		}

		void assign(string name, Token tk, bType t)
		{
			if(variables.ContainsKey(name))
			{
				throw new ErrorException($"Variable '{name}' has already been declared previously.");
			}
			variables[name] = t;

			switch(t)
			{
				case bType.INT:
					if(tk.lex != Lexeme.NUMBER)
					{
						throw new ErrorException($"Cannot assign '{tk.lex}' to a variable of type INT.");
					}
					try
					{
						intVars[name] = Convert.ToInt32(Convert.ToSingle(tk.literal));
					}
					catch(FormatException)
					{
						throw new ErrorException($"Cannot assign '{tk.literal}' to type INT.");
					}
					break;
				case bType.FLOAT:
					if(tk.lex != Lexeme.NUMBER)
					{
						throw new ErrorException($"Cannot assign '{tk.lex}' to a variable of type FLOAT.");
					}
					floatVars[name] = float.Parse(tk.literal);
					break;
				case bType.CHAR:
					if(tk.lex != Lexeme.CHARACTER)
					{
						throw new ErrorException($"Cannot assign '{tk.lex}' to a variable of type CHAR.");
					}
					charVars[name] = char.Parse(tk.literal);
					break;
				case bType.BOOL:
					if(tk.lex != Lexeme.BOOLEAN)
					{
						throw new ErrorException($"Cannot assign '{tk.lex}' to a variable of type BOOL.");
					}
					boolVars[name] = tk.literal == "TRUE" ? true : false;
					break;
			}
		}

		// void assignTo(string name, Token tk)
		// {
		// 	Type t = variables[name];

		// 	switch(t)
		// 	{
		// 		case float.GetType():
		// 			break;
		// 	}
		// }

		void checkDeclare(int index)
		{
			int state = 0;
			int x = index;

			while(tokenList[x].lex != Lexeme.NEWLINE)
			{
				switch(tokenList[x].lex)
				{
					case Lexeme.VAR:
						state = DeclareDFA[state, 0];
						break;
					case Lexeme.IDENTIFIER:
						state = DeclareDFA[state, 1];
						break;
					case Lexeme.ASSIGN:
						state = DeclareDFA[state, 2];
						break;
					case Lexeme.COMMA:
						state = DeclareDFA[state, 3];
						break;
					case Lexeme.STRING:
					case Lexeme.NUMBER:
					case Lexeme.CHARACTER:
					case Lexeme.BOOLEAN:
						state = DeclareDFA[state, 4];
						break;
					case Lexeme.AS:
						state = DeclareDFA[state, 5];
						break;
					case Lexeme.INT:
					case Lexeme.FLOAT:
					case Lexeme.CHAR:
					case Lexeme.BOOL:
						state = DeclareDFA[state, 6];
						break;
					default:
						state = -1;
						break;
				}

				if(state == -1)
				{
					throw new ErrorException($"Illegal {tokenList[x].lex} on line {tokenList[x].line}.");
				}

				x++;
			}
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