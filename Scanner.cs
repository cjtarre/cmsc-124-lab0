public class Scanner
{
    private readonly string source;
    private readonly List<Token> tokens = new();

    private bool hadError = false;

    public bool HadError => hadError;

    private static readonly Dictionary<string, TokenType> keywords = new()
    {
        ["INITIALIZE"] = TokenType.VAR,
        ["SET"] = TokenType.VAR,
        ["INPUT"] = TokenType.INPUT,
        ["OUTPUT"] = TokenType.OUTPUT,
        ["PRINT"] = TokenType.PRINT,

        ["IF"] = TokenType.IF,
        ["THEN"] = TokenType.THEN,
        ["ELSE"] = TokenType.ELSE,

        ["FOR"] = TokenType.FOR,
        ["EACH"] = TokenType.EACH,
        ["IN"] = TokenType.IN,
        ["TO"] = TokenType.TO,
        ["STEP"] = TokenType.STEP,

        ["WHILE"] = TokenType.WHILE,
        ["DO"] = TokenType.DO,

        ["REPEAT"] = TokenType.REPEAT,
        ["UNTIL"] = TokenType.UNTIL,

        ["CASE"] = TokenType.CASE,
        ["OF"] = TokenType.OF,
        ["DEFAULT"] = TokenType.DEFAULT,

        ["FUNCTION"] = TokenType.FUNCTION,
        ["PROCEDURE"] = TokenType.PROCEDURE,
        ["CALL"] = TokenType.CALL,
        ["RETURN"] = TokenType.RETURN,

        ["END"] = TokenType.END,

        ["TRUE"] = TokenType.TRUE,
        ["FALSE"] = TokenType.FALSE,
        ["NIL"] = TokenType.NIL,

        ["AND"] = TokenType.AND,
        ["OR"] = TokenType.OR,
        ["NOT"] = TokenType.BANG,
        ["XOR"] = TokenType.XOR,

        ["MOD"] = TokenType.MOD
    };

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

    private bool IsAlpha(char character)
    {
        return (character >= 'a' && character <= 'z')
            || (character >= 'A' && character <= 'Z')
            || character == '_';
    }

    private bool IsAlphaNumeric(char character)
    {
        return IsAlpha(character)
            || (character >= '0' && character <= '9');
    }

    public List<Token> ScanTokens()
    {
        while (!IsAtEnd())
        {
            start = current;
            ScanToken();
        }

        tokens.Add(new Token(TokenType.EOF, "", null, line));

        return tokens;
    }

    private void ScanToken()
    {
        char character = Advance();

        if (character == '(')
        {
            AddToken(TokenType.LEFT_PAREN);
        }
        else if (character == ')')
        {
            AddToken(TokenType.RIGHT_PAREN);
        }
        else if (character == '+')
        {
            AddToken(TokenType.PLUS);
        }
        else if (character == '-')
        {
            AddToken(TokenType.MINUS);
        }
        else if (character == '*')
        {
            AddToken(TokenType.STAR);
        }
        else if (character == '/')
        {
            AddToken(TokenType.SLASH);
        }
        else if (character == '=')
        {
            AddToken(TokenType.EQUAL_EQUAL);
        }
        else if (character == '!')
        {
            if (Match('='))
            {
                AddToken(TokenType.BANG_EQUAL);
            }
            else
            {
                AddToken(TokenType.BANG);
            }
        }
        else if (character == '<')
        {
            if (Match('='))
            {
                AddToken(TokenType.LESS_EQUAL);
            }
            else if (Match('-'))
            {
                AddToken(TokenType.EQUAL);
            }
            else
            {
                AddToken(TokenType.LESS);
            }
        }
        else if (character == '>')
        {
            if (Match('>'))
            {
                while (Peek() != '\n' && !IsAtEnd())
                {
                    Advance();
                }
            }
            else if (Match('='))
            {
                AddToken(TokenType.GREAT_EQUAL);
            }
            else
            {
                AddToken(TokenType.GREAT);
            }
        }
        else if (character == ' ' || character == '\t' || character == '\r')
        {
            return;
        }
        else if (character == '\n')
        {
            line++;
            return;
        }
        else if (character == '"')
        {
            String();
        }
        else if (character >= '0' && character <= '9')
        {
            Number();
        }
        else if (IsAlpha(character))
        {
            Identifier();
        }
    }

    private void Identifier()
    {
        while (IsAlphaNumeric(Peek()))
        {
            Advance();
        }

        string text = source[start..current];

        if (keywords.TryGetValue(text, out TokenType type))
        {
            if (type == TokenType.TRUE)
            {
                AddToken(TokenType.TRUE, true);
            }
            else if (type == TokenType.FALSE)
            {
                AddToken(TokenType.FALSE, false);
            }
            else
            {
                AddToken(type);
            }
        }
        else
        {
            AddToken(TokenType.IDENTIFIER);
        }
    }

    private void String()
    {
        while (Peek() != '"' && !IsAtEnd())
        {
            if (Peek() == '\n')
            {
                line++;
            }

            Advance();
        }

        if (IsAtEnd())
        {
            hadError = true;
            Console.Error.WriteLine(
                $"[Line {line}] Lexical Error: Unterminated string.");
            return;
        }

        Advance();

        string value = source[(start + 1)..(current - 1)];
        AddToken(TokenType.STRING, value);
    }

    private void Number()
    {
        while (Peek() >= '0' && Peek() <= '9')
        {
            Advance();
        }

        if (Peek() == '.' && current + 1 < source.Length
            && source[current + 1] >= '0'
            && source[current + 1] <= '9')
        {
            Advance();

            while (Peek() >= '0' && Peek() <= '9')
            {
                Advance();
            }
        }

        double value = double.Parse(source[start..current]);
        AddToken(TokenType.NUMBER, value);
    }

    private void AddToken(TokenType type)
    {
        string lexeme = source[start..current];
        tokens.Add(new Token(type, lexeme, null, line));
    }

    private void AddToken(TokenType type, object literal)
    {
        string lexeme = source[start..current];
        tokens.Add(new Token(type, lexeme, literal, line));
    }
}
