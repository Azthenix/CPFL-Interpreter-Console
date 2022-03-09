public class Token
{
    public TokenType type {get;}
    public string literal {get;}
    public int line {get;}

    public Token(TokenType type, string literal, int line)
    {
        this.type = type;
        this.literal = literal;
        this.line = line;
    }
}