class Token{
    public TokenType Type { get; }
    public string Lexeme { get; }
    public object? Literal { get; }
    public int Line { get; }

    public Token(TokenType type, string lexeme, object? literal, int line){
        Type = type;
        Lexeme = lexeme;
        Literal = literal;
        Line = line;
    }

    public override string ToString(){
        string literalText = Literal switch {
            null => "null",
            bool b => b.ToString().ToLower(),
            double d => (d % 1 == 0) 
                ? d.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)
                : d.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
            _ => Literal.ToString()!
        };
        return $"Token(type={Type}, lexeme={Lexeme}, literal={literalText}, line={Line})";
    }
}