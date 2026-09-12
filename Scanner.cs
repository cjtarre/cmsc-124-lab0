public class Scanner
{
    private readonly string source;
    private readonly List<Token> tokens = new();

    private int start = 0;
    private int current = 0;
    private int line = 1;

    public Scanner(string source)
    {
        this.source = source;
    }

    private char Advance()
    {
        char character = source[current];
        current++;
        return character;
    }

    private bool IsAtEnd()
    {
        return current >= source.Length;
    }

    private char Peek()
    {
        if (IsAtEnd())
        {
            return '\0';
        }

        return source[current];
    }

    private bool Match(char expected)
    {
        if (IsAtEnd())
        {
            return false;
        }

        if (source[current] != expected)
        {
            return false;
        }

        current++;
        return true;
    }
}