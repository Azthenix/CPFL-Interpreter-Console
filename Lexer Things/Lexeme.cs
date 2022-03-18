public enum Lexeme
{
    //Symbols ()\[]*/+-%><=,\n
    LPAR, RPAR,
    QUOTE, DQUOTE,
    LBRACK, RBRACK,
    AST, FSLASH, PLUS, MINUS, UAST, UFSLASH, UPLUS, UMINUS, UPERCENT,
    PERCENT, AMP,
    GREATER, LESSER, EQUAL, GEQUAL, LEQUAL, NEQUAL,
    ASSIGN,
    COMMA,
    COLON,
    SHARP,
    NEWLINE,

    //Data
    IDENTIFIER,
    STRING, NUMBER, CHARACTER, BOOLEAN,

    //Reserved
    INT, CHAR, BOOL, FLOAT, AND, OR, NOT, TRUE, FALSE, WHILE, IF, ELSE,
	VAR, AS, START, STOP, OUTPUT, INPUT
}