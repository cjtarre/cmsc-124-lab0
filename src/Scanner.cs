class Scanner {
    // source code to scan
    private readonly string _source;
    private readonly List<Token> _tokens = new List<Token>();

    // marker references
    private int _current = 0;
    private int _start = 0;
    private int _line = 1;

    public Scanner(string source){ 
        _source = source;
    }
    
    // helper methods
    public bool IsAtEnd() {
        return _current >= _source.Length;
    }

    public void AddToken(TokenType type, object? literal = null) {
        string text = _source.Substring(_start, _current - _start);
        _tokens.Add(new Token(type, text, literal, _line));
    }
    
    public char Advance() {
        return _source[_current++];
    }

    public char Peek(int offset = 0) {
        if (IsAtEnd()) return '\0';
        return _source[_current + offset];
    }
    
    public bool Match(char expected) {
        if (IsAtEnd()) return false;
        if (_source[_current] != expected) return false;
        _current++;
        return true;
    }
    
    // core scanner methods
    public void ScanTokens() {
        while (!IsAtEnd()) {
            _start = _current;
            ScanToken();
        }
        _start = _current;
        AddToken(TokenType.EOF);

        foreach (Token token in _tokens) {
            Console.WriteLine(token.ToString());
        }
    }

    public void ScanToken() {
        char c = Advance();
        switch (c) {
            // single character tokens
            case '(': AddToken(TokenType.LEFT_PAREN); break;
            case ')': AddToken(TokenType.RIGHT_PAREN); break;
            case ',': AddToken(TokenType.COMMA); break;
            case ':': AddToken(TokenType.COLON); break;
            case '.': AddToken(TokenType.DOT); break;
            case '=': AddToken(TokenType.EQUAL); break;
            case '+': AddToken(TokenType.PLUS); break;
            case '-': AddToken(TokenType.MINUS); break;
            case '*': AddToken(TokenType.STAR); break;
            case '/': AddToken(TokenType.SLASH); break;
            case '%': AddToken(TokenType.MOD); break;

            // tokens that can be more than one character
            case '<':
                if (Match('=')) {
                    AddToken(TokenType.LESSER_EQUAL);
                } else if (Match('-')) {
                    AddToken(TokenType.ASSIGN);
                } else {
                    AddToken(TokenType.LESSER);
                }
                break;
            case '>':
                if (Match('=')) {
                    AddToken(TokenType.GREATER_EQUAL);
                } else if (Match('>')){     // skip comments starting with ">>"
                    while (Peek() != '\n' && !IsAtEnd()) {
                        Advance();
                    }
                } else {
                    AddToken(TokenType.GREATER);
                }
                break;
            case '!':
                if (Match('=')) {
                    AddToken(TokenType.NOT_EQUAL);
                } else {
                    AddToken(TokenType.NOT);
                }
                break;
            case '&':
                if (Match('&')) {
                    AddToken(TokenType.AND);
                } else {
                    throw new Exception($"[Line {_line}] Lexical Error: Unexpected character '{c}' found in scan block.");
                }
                break;
            case '|':
                if (Match('|')) {
                    AddToken(TokenType.OR);
                } else {
                    throw new Exception($"[Line {_line}] Lexical Error: Unexpected character '{c}' found in scan block.");
                }
                break;
            case '"':
                ScanString();
                break;

            default:
                if (char.IsWhiteSpace(c)) {
                    if (c == '\n') _line++;
                } 
                else if (char.IsLetter(c) || c == '_') {
                    ScanKeyword();
                }
                else if (char.IsDigit(c)) {
                    ScanNumber();
                }    
                else {
                    throw new Exception($"[Line {_line}] Lexical Error: Unexpected character '{c}' found in scan block.");
                }
                break;
        }
    }

    private void ScanKeyword() {
        while ((char.IsLetterOrDigit(Peek()) || Peek() == '_') && !IsAtEnd()) {
            Advance();
        }
        string text = _source.Substring(_start, _current - _start);
        TokenType type = text switch {
            "INITIALIZE" => TokenType.INITIALIZE,
            "SET" => TokenType.SET,
            "INPUT" => TokenType.INPUT,
            "OUTPUT" => TokenType.OUTPUT,
            "PRINT" => TokenType.PRINT,
            "IF" => TokenType.IF,
            "THEN" => TokenType.THEN,
            "ELSE" => TokenType.ELSE,
            "FOR" => TokenType.FOR,
            "EACH" => TokenType.EACH,
            "IN" => TokenType.IN,
            "TO" => TokenType.TO,
            "STEP" => TokenType.STEP,
            "WHILE" => TokenType.WHILE,
            "DO" => TokenType.DO,
            "REPEAT" => TokenType.REPEAT,
            "UNTIL" => TokenType.UNTIL,
            "CASE" => TokenType.CASE,
            "OF" => TokenType.OF,
            "DEFAULT" => TokenType.DEFAULT,
            "FUNCTION" => TokenType.FUNCTION,
            "PROCEDURE" => TokenType.PROCEDURE,
            "CALL" => TokenType.CALL,
            "RETURN" => TokenType.RETURN,
            "END" => TokenType.END,
            "TRUE" => TokenType.TRUE,
            "FALSE" => TokenType.FALSE,
            "NIL" => TokenType.NIL,
            "AND" => TokenType.AND,
            "OR" => TokenType.OR,
            "NOT" => TokenType.NOT,
            "MOD" => TokenType.MOD,
            "XOR" => TokenType.XOR,
            _ => TokenType.IDENTIFIER
        };
        switch (type) {
            case TokenType.TRUE:
                AddToken(type, true);
                break;
            case TokenType.FALSE:
                AddToken(type, false);
                break;
            case TokenType.NIL:
                AddToken(type, null);
                break;
            default:
                AddToken(type);
                break;
        }
    }

    private void ScanString() {
        while (Peek() != '"' && !IsAtEnd()) {
            if (Peek() == '\n') _line++;
            Advance();
        }

        if (IsAtEnd()) {
            throw new Exception($"[Line {_line}] Unterminated String Error: expected closing \"");
        }

        // consume the closing "
        Advance();

        // trim the surrounding quotes
        string value = _source.Substring(_start + 1, _current - _start - 2);
        AddToken(TokenType.STRING, value);
    }
    
    private void ScanNumber() {
        while (char.IsDigit(Peek())) Advance();

        // check if number is fractional (contains a decimal point)
        if (Peek() == '.' && char.IsDigit(Peek(1))) {
            Advance();      // consume the "."
            while (char.IsDigit(Peek())) Advance();
        }

        string text = _source.Substring(_start, _current - _start);
        double value = double.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
        AddToken(TokenType.NUMBER, value);
    }
}