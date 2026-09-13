class Token{
    public TokenType type;
    public string lexeme;
    public object? literal;
    public int line;

    public Token(TokenType type, string lexeme, object? literal, int line){
        this.type = type;
        this.lexeme = lexeme;
        this.literal = literal;
        this.line = line;
    }

    public override string ToString(){
        string literalText = literal switch {
            null => "null",
            bool b => b.ToString().ToLower(),
            double d => d.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),
            _ => literal.ToString()!
        };

        return $"Token(type={type}, lexeme={lexeme}, literal={literalText}, line={line})";
    }
}