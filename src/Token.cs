using System.Globalization;

public record Token(TokenType Type, string Lexeme, object? Literal, int Line){
    public override string ToString(){
        string literalText = Literal switch {
            null => "null",
            bool b => b.ToString().ToLower(),
            double d => (d % 1 == 0) 
                ? d.ToString("F1", CultureInfo.InvariantCulture)
                : d.ToString("G", CultureInfo.InvariantCulture),
            _ => Literal.ToString()!
        };
        return $"Token(type={Type}, lexeme={Lexeme}, literal={literalText}, line={Line})";
    }
}