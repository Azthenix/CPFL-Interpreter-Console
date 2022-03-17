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

        int[,] DeclareDFA = new int[7, 7]{
            {1, -1, -1, -1, -1, -1, -1},
            {-1, 2, -1, -1, -1, -1, -1},
            {-1, -1, 3, 1, -1, 5, -1},
            {-1, 4, -1, -1, 4, -1, -1},
            {-1, -1, -1, 1, -1, 5, -1},
            {-1, -1, -1, -1, -1, -1, 6},
            {-1, -1, -1, -1, -1, -1, -1}
        };

        int[,] outputDFA = new int[4, 4]{
            {1, -1, -1, -1},
            {-1, 2, -1, -1},
            {-1, -1, 3, -1},
            {-1, -1, -1, 2}
        };

        int[,] checkNumAssDFA = new int[6, 6]{
            {1, -1, -1, -1, -1, -1},
            {-1, 2, -1, -1, -1, -1},
            {3, -1, 5, 4, -1, -1},
            {-1, 2, -1, -1, -1, 4},
            {5, -1, 5, 4, -1, -1},
            {-1, -1, -1, -1, 5, 4}
        };

        int[,] checkCharAssDFA = new int[6, 3]{
            {1, -1, -1},
            {-1, 2, -1},
            {3, -1, 5},
            {-1, 4, -1},
            {3, -1, 5},
            {-1, -1, -1}
        };

        int[,] checkBoolAssDFA = new int[9, 10]{
            {1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            {-1, 2, -1, -1, -1, -1, -1, -1, -1, -1},
            {3, -1, 5, 7, 4, -1, 4, -1, -1, -1},
            {-1, 2, -1, -1, -1, -1, -1, -1, -1, -1},
            {5, -1, 5, 7, 4, -1, 4, -1, -1, -1},
            {-1, -1, -1, -1, -1, -1, -1, 6, -1, 8},
            {7, -1, 7, -1, -1, -1, -1, -1, -1, -1},
            {-1, -1, -1, -1, -1, 7, -1, -1, 4, -1},
            {5, -1, 5, -1, -1, -1, -1, -1, -1, -1}
        };

        int[,] inputDFA = new int[4, 4]{
            {1, -1, -1, -1},
            {-1, 2, -1, -1},
            {-1, -1, 3, -1},
            {-1, -1, -1, 2},
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
        List<string> symbolsArray = new List<string> { "(", ")", "[", "]", "*", "/", "+", "-", "%", "&", ">", "<", "==", ">=", "<=", "=", "," };
        List<string> reserved = new List<string>{"INT", "CHAR", "BOOL", "FLOAT", "AND", "OR", "NOT", "WHILE", "TRUE", "FALSE",
                                        "VAR", "AS", "START", "STOP", "OUTPUT", "INPUT"};

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

            lines = File.ReadAllText(file)
            .Replace("\"TRUE\"", "TRUE")
            .Replace("\"FALSE\"", "FALSE")
            .Replace("[\"]", "$DQUOTE$")
            .Replace("\r", "")
            .Split('\n');

            for (int x = 0; x < lines.Length; x++)
            {
                string[] dqsplit = lines[x].Split('\"');

                for (int y = 1; y < dqsplit.Length; y += 2)
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
        }

        void Parse()
        {
            int ctr = 1;
            foreach (string line in lines)
            {
                string ln = line.Trim();

                if (ln.Length == 0 || ln[0] == '*')
                {
                    ctr++;
                    continue;
                }

                StringBuilder lit = new StringBuilder();

                for (int x = 0; x < ln.Length; x++)
                {
                    if (Char.IsLetterOrDigit(ln[x]) || ln[x] == '_' || ln[x] == '.')
                    {
                        lit.Append(ln[x]);
                    }
                    else
                    {
                        if (symbols.Contains(ln[x]))
                        {
                            if (lit.Length > 0)
                                addToken(lit.ToString(), ctr);

                            switch (ln[x])
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
                                    if (x < ln.Length && ln[x + 1] == '=')
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
                                    if (x < ln.Length && ln[x + 1] == '=')
                                    {
                                        tokenList.Add(new Token(Lexeme.LEQUAL, null, ctr));
                                        tokenLiterals.Add("<=");
                                        x++;
                                    }
                                    else if (x < ln.Length && ln[x + 1] == '>')
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
                                    if (x < ln.Length && ln[x + 1] == '=')
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
                                        while (ln[++x] != '\"')
                                        {
                                            if (ln[x] == '[' || ln[x] == ']')
                                            {
                                                throw new ErrorException($"Illegal escape code '{ln[x]}' on line {ctr}.");
                                            }

                                            lit.Append(ln[x]);
                                        }
                                        tokenList.Add(new Token(Lexeme.STRING, lit.ToString(), ctr));
                                        lit.Clear();
                                    }
                                    catch (IndexOutOfRangeException)
                                    {
                                        throw new ErrorException($"Missing '\"' on line {ctr}.");
                                    }
                                    lit.Clear();
                                    break;
                                default:
                                    lit.Clear();
                                    try
                                    {
                                        if (ln[x + 2] == '\'')
                                        {
                                            if (ln[x + 1] == '[' || ln[x + 1] == ']' || ln[x + 1] == '\"')
                                            {
                                                throw new ErrorException($"Illegal character '{ln[x + 1]}' on line {ln[x + 1]}.");
                                            }
                                            lit.Append(ln[x + 1]);
                                            tokenList.Add(new Token(Lexeme.CHARACTER, ln[x + 1] == '#' ? "\n" : lit.ToString(), ctr));

                                            x += 2;
                                        }
                                        else if (ln[x + 4] == '\'')
                                        {
                                            string sub = ln.Substring(x + 1, 3);
                                            if (sub == "[[]" || sub == "[]]" || sub == "[\"]" || sub == "[#]")
                                            {
                                                tokenList.Add(new Token(Lexeme.CHARACTER, sub, ctr));
                                            }

                                            x += 4;
                                        }
                                        lit.Clear();
                                    }
                                    catch (IndexOutOfRangeException)
                                    {
                                        throw new ErrorException($"Missing ''' on line {ln[x + 1]}.");
                                    }
                                    break;
                            }

                            lit.Clear();
                        }
                        else if (ln[x] == ' ')
                        {
                            if (lit.Length > 0)
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

                if (lit.Length > 0)
                    addToken(lit.ToString(), ctr);

                tokenList.Add(new Token(Lexeme.NEWLINE, null, ctr));
                ctr += 1;
            }
        }

        void addToken(string literal, int ctr)
        {
            if (reserved.Contains(literal))
            {
                if (literal == "TRUE" || literal == "FALSE")
                {
                    tokenList.Add(new Token(Lexeme.BOOLEAN, literal, ctr));
                }
                else
                {
                    tokenList.Add(new Token(Enum.Parse<Lexeme>(literal), null, ctr));
                }
                return;
            }
            if (Char.IsDigit(literal[0]))
            {
                if (!literal.All(x => Char.IsDigit(x) || x == '.'))
                {
                    throw new ErrorException($"Illegal Identifier '{literal}' on line {ctr}.");
                }

                tokenList.Add(new Token(Lexeme.NUMBER, literal, ctr));
                return;
            }
            if (Char.IsLetter(literal[0]) || literal[0] == '_')
            {
                if (!literal.All(x => Char.IsLetterOrDigit(x) || x == '_'))
                {
                    throw new ErrorException($"Illegal Identifier '{literal}' on line {ctr}.");
                }

                tokenList.Add(new Token(Lexeme.IDENTIFIER, literal, ctr));
                return;
            }

            throw new ErrorException($"Illegal Identifier '{literal}' on line {ctr}.");
        }

        void Interpret()
        {
            List<Token> tks = new List<Token>(tokenList);

            for (int x = 0; x < tks.Count; x++)
            {
                switch (tks[x].lex)
                {
                    case Lexeme.VAR:
                        checkDeclare(x);
                        Declare(x, ref x);
                        break;
                    case Lexeme.START:
                        x++;
                        executeBody(x, ref x, false);
                        break;
                    case Lexeme.NEWLINE:
                        break;
                    default:
                        throw new ErrorException($"Illegal '{tks[x].lex}' on line {tks[x].line}.");
                }
            }
        }

        void executeBody(int index, ref int y, bool skip)
        {
            int x = index;

            int skipTo;

            for (; x < tokenList.Count; x++)
            {
                switch (tokenList[x].lex)
                {
                    case Lexeme.IDENTIFIER:
                        if (tokenList[x + 1].lex != Lexeme.ASSIGN)
                        {
                            throw new ErrorException($"Illegal '{tokenList[x].literal}' on line {tokenList[x].line}.");
                        }

                        switch (variables[tokenList[x].literal])
                        {
                            case bType.INT:
                            case bType.FLOAT:
                                skipTo = checkAssignToNum(x);
                                if (skip)
                                {
                                    x = skipTo;
                                }
                                else
                                {
                                    assignToNum(x, ref x);
                                }
                                break;
                            case bType.CHAR:
                                skipTo = checkAssignToChar(x);
                                if (skip)
                                {
                                    x = skipTo;
                                }
                                else
                                {
                                    assignToChar(x, ref x);
                                }
                                break;
                            case bType.BOOL:
                                skipTo = checkAssignToBool(x);
                                if (skip)
                                {
                                    x = skipTo;
                                }
                                else
                                {
                                    assignToBool(x, ref x);
                                }
                                break;
                        }
                        break;
                    case Lexeme.START:
                        executeBody(x + 1, ref x, skip);
                        break;
                    case Lexeme.WHILE:
                        int loopIn = x;
                        bool loop = evaluateBool(x + 1, ref x);
                        x += 1;
                        if (tokenList[x].lex == Lexeme.START)
                        {
                            executeBody(x + 1, ref x, skip == loop);
                        }
                        else
                        {
                            throw new ErrorException($"Illegal '{tokenList[x].lex}' on line {tokenList[x].line}.");
                        }
                        if (!skip && loop)
                        {
                            x = loopIn - 1;
                        }
                        break;
                    case Lexeme.STOP:
                        y = x + 1;
                        return;
                    case Lexeme.OUTPUT:
                        skipTo = checkOutput(x);
                        if (skip)
                        {
                            x = skipTo;
                        }
                        else
                        {
                            Output(x, ref x);
                        }
                        break;
                    case Lexeme.INPUT:
                        checkInput(x);
                        Input(x, ref x);
                        break;
                    case Lexeme.NEWLINE:
                        break;
                    default:
                        throw new ErrorException($"Illegal '{tokenList[x].lex}' on line {tokenList[x].line}.");
                }
            }

            y = x + 1;
        }

        void Declare(int index, ref int y)
        {
            int x = index;
            List<string> names = new List<string>();
            bType t = bType.INT;

            while (tokenList[x].lex != Lexeme.NEWLINE)
            {
                switch (tokenList[x].lex)
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

            while (x > index)
            {
                if (tokenList[x].lex == Lexeme.IDENTIFIER)
                {
                    string name = tokenList[x].literal;

                    if (variables.ContainsKey(name))
                    {
                        throw new ErrorException($"Variable '{name}' has already been declared previously.");
                    }

                    variables[name] = t;
                }
                else if (tokenList[x].lex == Lexeme.ASSIGN)
                {
                    assign(tokenList[x - 1].literal, tokenList[x + 1], t);
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

            if (tokenList[x + 1].lex == Lexeme.ASSIGN)
            {
                if (tokenList[x].lex == Lexeme.IDENTIFIER)
                {
                    res = assignToNum(x, ref x);

                    switch (variables[tokenList[index].literal])
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

            while (tokenList[x].lex != Lexeme.NEWLINE)
            {
                switch (tokenList[x].lex)
                {
                    case Lexeme.IDENTIFIER:
                        try
                        {
                            switch (variables[tokenList[x].literal])
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
                        catch (KeyNotFoundException)
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

            switch (variables[tokenList[index].literal])
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

        int checkAssignToNum(int index)
        {
            int state = 0;
            int x = index;
            int pars = 0;

            while (tokenList[x].lex != Lexeme.NEWLINE)
            {
                switch (tokenList[x].lex)
                {
                    case Lexeme.IDENTIFIER:
                        state = checkNumAssDFA[state, 0];
                        break;
                    case Lexeme.ASSIGN:
                        state = checkNumAssDFA[state, 1];
                        break;
                    case Lexeme.NUMBER:
                        state = checkNumAssDFA[state, 2];
                        break;
                    case Lexeme.LPAR:
                        pars++;
                        state = checkNumAssDFA[state, 3];
                        break;
                    case Lexeme.RPAR:
                        if (--pars < 0)
                        {
                            throw new ErrorException($"Illegal ')' on line {tokenList[x].line}.");
                        }
                        state = checkNumAssDFA[state, 4];
                        break;
                    case Lexeme.AST:
                    case Lexeme.FSLASH:
                    case Lexeme.PLUS:
                    case Lexeme.MINUS:
                    case Lexeme.PERCENT:
                        state = checkNumAssDFA[state, 5];
                        break;
                    default:
                        state = -1;
                        break;
                }

                if (state == -1)
                {
                    throw new ErrorException($"Illegal {tokenList[x].lex} on line {tokenList[x].line}.");
                }

                x++;
            }

            if (state != 3 && state != 5)
                throw new ErrorException($"Invalid assignment on line {tokenList[x].line}.");

            return x;
        }

        char assignToChar(int index, ref int y)
        {
            int x = index;
            char res;

            x += 2;

            if (tokenList[x + 1].lex == Lexeme.ASSIGN)
            {
                if (tokenList[x].lex == Lexeme.IDENTIFIER)
                {
                    res = assignToChar(x, ref x);
                    charVars[tokenList[index].literal] = res;

                    y = x;

                    return res;
                }
            }

            if (tokenList[x].lex == Lexeme.IDENTIFIER)
            {
                res = charVars[tokenList[x].literal];
            }
            else
            {
                res = tokenList[x].literal[0];
            }

            y = x;

            charVars[tokenList[index].literal] = res;

            return res;
        }

        int checkAssignToChar(int index)
        {
            int state = 0;
            int x = index;

            while (tokenList[x].lex != Lexeme.NEWLINE)
            {
                switch (tokenList[x].lex)
                {
                    case Lexeme.IDENTIFIER:
                        state = checkCharAssDFA[state, 0];
                        break;
                    case Lexeme.ASSIGN:
                        state = checkCharAssDFA[state, 1];
                        break;
                    case Lexeme.CHARACTER:
                        state = checkCharAssDFA[state, 2];
                        break;
                    default:
                        state = -1;
                        break;
                }

                if (state == -1)
                {
                    throw new ErrorException($"Illegal {tokenList[x].lex} on line {tokenList[x].line}.");
                }

                x++;
            }

            if (state != 3 && state != 5)
                throw new ErrorException($"Invalid assignment on line {tokenList[x].line}.");

            return x;
        }

        bool assignToBool(int index, ref int y)
        {
            int x = index;
            bool res;

            x += 2;

            if (tokenList[x + 1].lex == Lexeme.ASSIGN)
            {
                if (tokenList[x].lex == Lexeme.IDENTIFIER)
                {
                    res = assignToBool(x, ref x);

                    boolVars[tokenList[index].literal] = res;

                    y = x;

                    return res;
                }
            }

            boolVars[tokenList[index].literal] = evaluateBool(x, ref x);

            y = x;

            return true;
        }

        bool evaluateBool(int index, ref int y)
        {
            bool res;

            int x = index;

            List<string> eval = new List<string>();

            while (tokenList[x].lex != Lexeme.NEWLINE)
            {
                switch (tokenList[x].lex)
                {
                    case Lexeme.IDENTIFIER:
                        try
                        {
                            switch (variables[tokenList[x].literal])
                            {
                                case bType.INT:
                                    eval.Add(intVars[tokenList[x].literal].ToString());
                                    break;
                                case bType.FLOAT:
                                    eval.Add(floatVars[tokenList[x].literal].ToString());
                                    break;
                                case bType.CHAR:
                                    eval.Add(charVars[tokenList[x].literal].ToString());
                                    break;
                                case bType.BOOL:
                                    eval.Add(boolVars[tokenList[x].literal].ToString());
                                    break;
                                default:
                                    throw new ErrorException($"Illegal IDENTIFIER '{tokenList[x].literal}' on line {tokenList[x].line}.");
                            }
                        }
                        catch (KeyNotFoundException)
                        {
                            throw new ErrorException($"Use of unassigned variable '{tokenList[x].literal}' on line {tokenList[x].line}.");
                        }
                        break;
                    case Lexeme.NUMBER:
                        eval.Add(tokenList[x].literal);
                        break;
                    case Lexeme.CHARACTER:
                        eval.Add($"'{tokenList[x].literal}'");
                        break;
                    case Lexeme.BOOLEAN:
                        eval.Add($"{tokenList[x].literal}");
                        break;
                    case Lexeme.LPAR:
                        eval.Add("(");
                        break;
                    case Lexeme.RPAR:
                        eval.Add(")");
                        break;
                    case Lexeme.AST:
                        eval.Add("*");
                        break;
                    case Lexeme.FSLASH:
                        eval.Add("/");
                        break;
                    case Lexeme.PLUS:
                        eval.Add("+");
                        break;
                    case Lexeme.MINUS:
                        eval.Add("-");
                        break;
                    case Lexeme.PERCENT:
                        eval.Add("%");
                        break;
                    case Lexeme.GREATER:
                        eval.Add(">");
                        break;
                    case Lexeme.LESSER:
                        eval.Add("<");
                        break;
                    case Lexeme.EQUAL:
                        eval.Add("=");
                        break;
                    case Lexeme.GEQUAL:
                        eval.Add(">=");
                        break;
                    case Lexeme.LEQUAL:
                        eval.Add("<=");
                        break;
                    case Lexeme.NEQUAL:
                        eval.Add("<>");
                        break;
                    case Lexeme.NOT:
                        eval.Add("NOT");
                        break;
                    default:
                        throw new ErrorException($"Illegal '{tokenList[x].lex}' on line {tokenList[x].line}.");
                }
                x++;
            }

            y = x;

            DataTable dt = new DataTable();

            res = (bool)dt.Compute(String.Join(' ', eval), "");

            return res;
        }

        int checkAssignToBool(int index)
        {
            int state = 0;
            int x = index;
            int pars = 0;

            while (tokenList[x].lex != Lexeme.NEWLINE)
            {
                switch (tokenList[x].lex)
                {
                    case Lexeme.IDENTIFIER:
                        state = checkBoolAssDFA[state, 0];
                        break;
                    case Lexeme.ASSIGN:
                        state = checkBoolAssDFA[state, 1];
                        break;
                    case Lexeme.STRING:
                    case Lexeme.NUMBER:
                    case Lexeme.CHARACTER:
                        state = checkBoolAssDFA[state, 2];
                        break;
                    case Lexeme.BOOLEAN:
                        state = checkBoolAssDFA[state, 3];
                        break;
                    case Lexeme.LPAR:
                        pars++;
                        state = checkBoolAssDFA[state, 4];
                        break;
                    case Lexeme.RPAR:
                        if (--pars < 0)
                        {
                            throw new ErrorException($"Illegal ')' on line {tokenList[x].line}.");
                        }
                        state = checkBoolAssDFA[state, 5];
                        break;
                    case Lexeme.NOT:
                        state = checkBoolAssDFA[state, 6];
                        break;
                    case Lexeme.GREATER:
                    case Lexeme.LESSER:
                    case Lexeme.EQUAL:
                    case Lexeme.GEQUAL:
                    case Lexeme.LEQUAL:
                    case Lexeme.NEQUAL:
                        state = checkBoolAssDFA[state, 7];
                        break;
                    case Lexeme.AND:
                    case Lexeme.OR:
                        state = checkBoolAssDFA[state, 8];
                        break;
                    case Lexeme.AST:
                    case Lexeme.FSLASH:
                    case Lexeme.PLUS:
                    case Lexeme.MINUS:
                    case Lexeme.PERCENT:
                        state = checkBoolAssDFA[state, 9];
                        break;
                    default:
                        state = -1;
                        break;
                }

                if (state == -1)
                {
                    throw new ErrorException($"Illegal {tokenList[x].lex} on line {tokenList[x].line}.");
                }

                x++;
            }

            if (pars != 0)
                throw new ErrorException($"Unclosed '(' on line {tokenList[x].line}.");

            if (state != 3 && state != 7)
                throw new ErrorException($"Invalid assignment on line {tokenList[x].line}.");

            return x;
        }

        void Output(int index, ref int y)
        {
            int x = index + 2;

            StringBuilder sb = new StringBuilder();

            while (tokenList[x].lex != Lexeme.NEWLINE)
            {
                switch (tokenList[x].lex)
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
                        switch (variables[tokenList[x].literal])
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

        int checkOutput(int index)
        {
            int state = 0;
            int x = index;

            while (tokenList[x].lex != Lexeme.NEWLINE)
            {
                switch (tokenList[x].lex)
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

                if (state == -1)
                {
                    throw new ErrorException($"Illegal {tokenList[x].lex} on line {tokenList[x].line}.");
                }

                x++;
            }

            return x;
        }

        // void Input()
        void assign(string name, Token tk, bType t)
        {
            if (variables.ContainsKey(name))
            {
                throw new ErrorException($"Variable '{name}' has already been declared previously.");
            }
            variables[name] = t;

            switch (t)
            {
                case bType.INT:
                    if (tk.lex != Lexeme.NUMBER)
                    {
                        throw new ErrorException($"Cannot assign '{tk.lex}' to a variable of type INT.");
                    }
                    try
                    {
                        intVars[name] = Convert.ToInt32(Convert.ToSingle(tk.literal));
                    }
                    catch (FormatException)
                    {
                        throw new ErrorException($"Cannot assign '{tk.literal}' to type INT.");
                    }
                    break;
                case bType.FLOAT:
                    if (tk.lex != Lexeme.NUMBER)
                    {
                        throw new ErrorException($"Cannot assign '{tk.lex}' to a variable of type FLOAT.");
                    }
                    floatVars[name] = float.Parse(tk.literal);
                    break;
                case bType.CHAR:
                    if (tk.lex != Lexeme.CHARACTER)
                    {
                        throw new ErrorException($"Cannot assign '{tk.lex}' to a variable of type CHAR.");
                    }
                    charVars[name] = char.Parse(tk.literal);
                    break;
                case bType.BOOL:
                    if (tk.lex != Lexeme.BOOLEAN)
                    {
                        throw new ErrorException($"Cannot assign '{tk.lex}' to a variable of type BOOL.");
                    }
                    boolVars[name] = tk.literal == "TRUE" ? true : false;
                    break;
            }
        }

        void checkDeclare(int index)
        {
            int state = 0;
            int x = index;

            while (tokenList[x].lex != Lexeme.NEWLINE)
            {
                switch (tokenList[x].lex)
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

                if (state == -1)
                {
                    throw new ErrorException($"Illegal {tokenList[x].lex} on line {tokenList[x].line}.");
                }

                x++;
            }
        }

        void checkInput(int index)
        {
            int state = 0;
            int x = index;

            while (tokenList[x].lex != Lexeme.NEWLINE)
            {
                switch (tokenList[x].lex)
                {
                    case Lexeme.INPUT:
                        state = inputDFA[state, 0];
                        break;
                    case Lexeme.COLON:
                        state = inputDFA[state, 1];
                        break;
                    case Lexeme.IDENTIFIER:
                        state = inputDFA[state, 2];
                        break;
                    case Lexeme.COMMA:
                        state = inputDFA[state, 3];
                        break;
                    default:
                        state = -1;
                        break;
                }

                if (state == -1)
                {
                    throw new ErrorException($"Illegal {tokenList[x].lex} on line {tokenList[x].line}.");
                }
                x++;
            }
        }

        void addInputToken(ref List<Token> inputList, string literal, int ctr)
        {

            if (reserved.Contains(literal))
            {
                if (literal == "TRUE" || literal == "FALSE")
                {
                    inputList.Add(new Token(Lexeme.BOOLEAN, literal, ctr));
                }
                else
                {
                    inputList.Add(new Token(Enum.Parse<Lexeme>(literal), null, ctr));
                }
                return;
            }
            if (Char.IsDigit(literal[0]))
            {
                if (!literal.All(x => Char.IsDigit(x) || x == '.'))
                {
                    throw new ErrorException($"Illegal Identifier '{literal}' on line {ctr}.");
                }

                inputList.Add(new Token(Lexeme.NUMBER, literal, ctr));
                return;
            }

            if (literal.Length == 1 && Char.IsLetter(literal[0]))
            {
                if (!literal.All(x => Char.IsLetter(x)))
                {
                    throw new ErrorException($"Illegal Identifier '{literal}' on line {ctr}.");
                }
                inputList.Add(new Token(Lexeme.CHARACTER, literal, ctr));
                return;
            }

            if (Char.IsLetter(literal[0]) || literal[0] == '_')
            {
                if (!literal.All(x => Char.IsLetterOrDigit(x) || x == '_'))
                {
                    throw new ErrorException($"Illegal Identifier '{literal}' on line {ctr}.");
                }
                inputList.Add(new Token(Lexeme.IDENTIFIER, literal, ctr));
                return;
            }

            throw new ErrorException($"Illegal Identifier '{literal}' on line {ctr}.");
        }

        void inputAssign(string name, Token tk, int i)
        {
            bType t = variables[name];

            switch (t)
            {
                case bType.INT:
                    if (tk.lex != Lexeme.NUMBER)
                    {
                        throw new ErrorException($"Cannot assign '{tk.lex}' to a variable of type INT.");
                    }
                    try
                    {
                        intVars[name] = Convert.ToInt32(Convert.ToSingle(tk.literal));
                    }
                    catch (FormatException)
                    {
                        throw new ErrorException($"Cannot assign '{tk.literal}' to type INT.");
                    }
                    break;
                case bType.FLOAT:
                    if (tk.lex != Lexeme.NUMBER)
                    {
                        throw new ErrorException($"Cannot assign '{tk.lex}' to a variable of type FLOAT.");
                    }
                    floatVars[name] = float.Parse(tk.literal);
                    break;
                case bType.CHAR:
                    if (tk.lex != Lexeme.CHARACTER)
                    {
                        throw new ErrorException($"Cannot assign '{tk.lex}' to a variable of type CHAR.");
                    }
                    charVars[name] = char.Parse(tk.literal);
                    break;
                case bType.BOOL:
                    if (tk.lex != Lexeme.BOOLEAN)
                    {
                        throw new ErrorException($"Cannot assign '{tk.lex}' to a variable of type BOOL.");
                    }
                    boolVars[name] = tk.literal == "TRUE" ? true : false;
                    break;
            }

        }
        void Input(int index, ref int y)
        {
            int x = index + 2;
            int ctr = 0;
            string[] inputList;
            List<Token> inputValue = new List<Token>();

            string input = Console.ReadLine();
            inputList = input.Split(',');


            foreach (string str in inputList)
            {
                string trimmed = str.Trim(' ', '\'', '\"');
                addInputToken(ref inputValue, trimmed, 0);
            }

            while (tokenList[x].lex != Lexeme.NEWLINE)
            {

                switch (tokenList[x].lex)
                {
                    case Lexeme.IDENTIFIER:
                        {
                            inputAssign(tokenList[x].literal.ToString(), inputValue[ctr], 0);
                            ctr++;
                            break;
                        }
                    case Lexeme.COMMA:
                        break;

                    default:
                        throw new ErrorException($"Illegal {tokenList[x].lex} on line {tokenList[x].line}.");

                }
                x++;
            }

            y = x;
        }


    }
}