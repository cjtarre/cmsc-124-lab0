class Parser {
    // fields and properties
    private readonly IReadOnlyList<Token> _tokens = [];
    public Parser(IReadOnlyList<Token> tokens) => _tokens = tokens;
    private int _current = 0;
    
    // helper methods
    private bool Match (params TokenType[] types) { // checks and advances if true
        foreach (TokenType type in types) {
            if (Check(type)) {
                Advance();
                return true;
            }
        }
        return false;
    }
    private bool IsAtEnd() => Peek().Type == TokenType.EOF;
    private bool Check(TokenType type) => Peek().Type == type;
    private Token Peek() => _tokens[_current]; 
    private Token Previous() => _tokens[_current - 1];  // returns the most recently consumed token
    private Token Advance() {
        if (!IsAtEnd()) _current++;
        return Previous();
    }
    private Token Consume(TokenType type, string message) {
        if (Check(type)) return Advance();
        throw Error(Peek(), message);
    }
    public void Parse() {
    } 
    private void Synchronize() {
    }
    private Expr Expression() => Assignment();  // entry point for parsing expressions
    private Expr Assignment() {
    }
    private Expr LogicalOr() {
    }
    private Expr LogicalAnd() {
    }
    private Expr LogicalNot() {
    }
    private Expr Comparison() {
    }
    private Expr Term() {
    }
    private Expr Factor() {
    }
    private Expr UnaryMinus() {
    }
    private Expr Power() {
    }
    private Expr Primary() {
    }
}