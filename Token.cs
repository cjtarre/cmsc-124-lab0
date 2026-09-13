public class Token
{
    public TokenType Type { get; }
    public string Lexeme { get; }
    public object? Literal { get; }
    public int Line { get; }

    public Token(TokenType type, string lexeme, object? literal, int line)
    {
        Type = type;
        Lexeme = lexeme;
        Literal = literal;
        Line = line;
    }

    public override string ToString()
    {
        return $"Token(type={Type}, lexeme={Lexeme}, literal={Literal switch
        {
            null => "null",
            double number => number.ToString("0.0"),
            bool boolean => boolean.ToString().ToLowerInvariant(),
            _ => Literal.ToString()
        }}, line={Line})";
    }
}
