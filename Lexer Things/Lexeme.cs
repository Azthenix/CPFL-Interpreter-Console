public enum Lexeme
{
    //Symbols ()\[]*/+-%><=,\n
    LPAR, RPAR,
    QUOTE, DQUOTE,
    LBRACK, RBRACK,
    AST, FSLASH, PLUS, MINUS,
    PERCENT, AMP,
    GREATER, LESSER, EQUAL, GEQUAL, LEQUAL, NEQUAL,
    ASSIGN,
    COMMA,
    SHARP,
    NEWLINE,

    //Data
    IDENTIFIER,
    CONSTANT,

    //Reserved
    INT, CHAR, BOOL, FLOAT, AND, OR, NOT, TRUE, FALSE,
	VAR, AS, START, STOP, OUTPUT
}