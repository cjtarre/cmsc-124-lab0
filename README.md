# `SudoCode`

## Creators
- Gabrielle Sumergido ([`freshlybakedsnep`](https://github.com/freshlybakedsnep))
- Ma. Christie Jude Tarre ([`cjtarre`](https://github.com/cjtarre))

## Overview
`SudoCode` is a simplified dynamically-typed language that helps programmers visualize algorithm behavior without being constrained by rigid syntactic overhead. It features a natural-language layout and a loosely structured design that mirrors classic academic pseudocode conventions. Writing in `SudoCode` should feel like drafting a flowchart or a textbook algorithm directly into an executable text file, allowing developers to focus entirely on core computational logic and sequence design rather than bracket tracking and typing safety. 

## Host language and build
- Host language: C# (.NET 10.0)
- Version metadata: `/SudoCode.csproj` (Target Framework: `net10.0`)
- Build: `./build.sh`
- A fresh clone requires the host machine to have the .NET 10.0 SDK (or later) installed. Running `./build.sh` triggers `dotnet build --configuration Release`.

## Running it
| Command | What it does |
|---|---|
| `./run <file>` | [Executes a program. Available from Lab 4.] |
| `./run --tokenize <file>` | Prints the token stream to standard output. |
| `./run --parse <file>` | Prints the parsed tree. |
| `./run --eval <file>` | [Evaluates each expression and prints its value.] |
| `./run` | [Starts the REPL.] |

Exit codes: 
- `0`: When a program runs or tokenizes completely without any lexical, syntactic, or execution failures. 
- `65`: When a static error is encountered (such as a lexical flaw or invalid character during tokenization).
- `70`: When a runtime exception is thrown during evaluation.

## File extension
`.sudo` - a stylized abbreviation of the word Pseudo.

## Lexical structure
### Keywords
All keywords in this language are defined and written in uppercase.
| Keyword | Purpose |
|---|---|
| `INITIALIZE` | Sets up a variable or data structure with an initial value using `<-`. |
| `SET` | Modifies the value of an existing variable using `<-`. |
| `INPUT` | Receives raw input streams from the tracking host environment context. |
| `OUTPUT` / `PRINT` | Displays stringified evaluation representations to the standard stdout terminal stream. |
| `IF` / `THEN` / `ELSE` | Controls conditional multi-branch logical flow paths. |
| `FOR` / `EACH` / `IN` / `TO` / `STEP` | Handles counter and range-based sequence loops. |
| `WHILE` / `DO` | Spawns traditional conditional execution iteration tracks. |
| `REPEAT` / `UNTIL` | Spawns standard bottom-driven boundary loop structures. |
| `CASE` / `OF` / `DEFAULT` | Manages structural multi-option expression selections. |
| `FUNCTION` / `PROCEDURE` / `CALL` / `RETURN` | Formulates explicit modular routine structures and scope executions. |
| `END` | Explicitly seals structural layout blocks (e.g., `END IF`, `END WHILE`). |

### Master Token Registry
```
LEFT_PAREN  RIGHT_PAREN
PLUS  MINUS  STAR  SLASH  MOD
EQUAL  EQUAL_EQUAL  BANG  BANG_EQUAL  LESS  LESS_EQUAL  GREAT  GREAT_EQUAL
IDENTIFIER  STRING  NUMBER
VAR  PRINT  IF  ELSE  WHILE  TRUE  FALSE  NIL
EOF
```
```
SET  INITIALIZE  INPUT  OUTPUT 
END  THEN  FOR  EACH  IN  TO  STEP DO  REPEAT  UNTIL  CASE  OF  DEFAULT 
CALL  RETURN  FUNCTION  PROCEDURE 
AND  OR  NOT  XOR
DOT  COLON  COMMA 
```

### Operators
| Operator | Category | Operands | Associativity | Precedence |
|---|---|---|---|---|
| `*`, `/`, `MOD` | arithmetic | binary | left | 5 (*tightest*) |
| `+`, `-` | arithmetic | binary | left | 4 |
| `=`, `>`, `>=`, `<`, `<=`, `!=` | comparison | binary | left | 3 |
| `NOT` | logical | unary | right | 2 |
| `AND`, `OR`, `XOR` | logical | binary | left | 1 |
| `<-` | assignment | binary | right | 0 (*loosest*) |

### Literals
| Kind | Syntax | Produces |
|---|---|---|
| `IDENTIFIER` | *see [`Identifiers`](#identifiers)* | A bound reference name to a stored variable context |
| `NUMBER` | Digits with optional single dot fractional notation (e.g., `42`, `3.14`) | IEEE 754 double-precision floating-point runtime values |
| `STRING` | Characters wrapped inside matching double quotation marks (e.g., `"hello"`) | UTF-16 character string runtime values |
| `BOOLEAN` | Case-sensitive Boolean keywords: `TRUE`, `FALSE` | Logic bit values (`true` / `false`) |
| `NIL` | The explicit empty value keyword `NIL` | Null pointer baseline references |

### Identifiers
- Start characters: Letters `a`-`z`, `A`-`Z`, or an underscore `_`
- Continue characters: Letters `a`-`z`, `A`-`Z`, numbers `0`-`9` or an underscore `_`
- Case-sensitive: **Yes**.
- Compound instructions like `END IF` or `ELSE IF` are scanned as independent tokens (`END` followed by `IF`) and handled at the grammar phase. 
- Variable names cannot perfectly match any singular keyword identifier list.
- String Boundaries: String literals must begin and end with explicit double quotes (`"`). Direct use of single quotes (`'`) are not recognized and will throw a lexical error.
- Multi-line Strings: String literals cannot span multiple lines. Hitting a newline character `\n` before finding the closing double quote will immediately terminate scanning and trigger an `Unterminated String` static error (Exit Code `65`).
- Escape Characters: `SudoCode` strings do not process backslash escape sequences (like `\n` or `\t`) within text literals. A backslash is treated as a literal text character.

### Comments
- Line comments: `>>`
- Block comments: Not supported
- Nesting: Not supported
- Harness note: `comment_prefix` in `tests/lab*/manifest.json` is set to "`>>`".

## Whitespace and termination
- Whitespace significant: **No**. Whitespace acts as a delimiter to distinguish individual words and symbols but does not dictate program structural grouping or indent validation.
- Statement terminator: **None**. Code steps are separated implicitly by statement block sequences and structural boundary wrappers. Newlines are treated as standard whitespace but are tracked for token line numbers.
- Block delimiters: **Marked explicitly** by corresponding keyword bounds <br>(e.g., `IF` ... `THEN` ... `END IF` or `WHILE` ... `DO` ... `END WHILE`).
- Grouping delimiters: Standard parentheses `(` and `)` are used to force expression precedence and enclose function/procedure parameters.
- Token separators: 
  - Commas `,` are used to separate parameters in function definitions.
  - Colons `:` mark branch blocks inside conditional `CASE` statements
  - Dots `.` are used as an explicit member access operator for dot notation structures.

## Token output format
```
Token(type=VAR, lexeme=INITIALIZE, literal=null, line=1)
Token(type=IDENTIFIER, lexeme=counter, literal=null, line=1)
Token(type=EQUAL, lexeme=<-, literal=null, line=1)
Token(type=NUMBER, lexeme=10, literal=10.0, line=1)
Token(type=EOF, lexeme=, literal=null, line=1)
```
- `type`: The internal enum categorization of the matched string token.
- `lexeme`: The literal exact substring sequence sliced directly from the `.sudo` raw file.
- `literal`: The interpreted object primitive representation (unboxed strings, parsed double precision values, or `null`).
- `line`: The tracker value pointing to the line number the token was detected on.

## Grammar
*To be defined in a later lab activity.*

## Parse output format
*To be defined in a later lab activity.*

## Semantics
*To be defined in a later lab activity.*

### Values and types
`SudoCode` supports four core runtime data primitive types managed dynamically in the host architecture:
- **Numbers**: Represented inside the host engine environment as native C# `double` precision variables for dynamic typing compatibility.
- **Strings**: Stored and tracked as native .NET `string` objects.
- **Booleans**: Represented via native C# `bool` flags (`true` and `false`).
- **Nil**: Represents an empty or uninitialized state, mapped straight to a native C# `null` reference value.

### Value printing
- Numbers: Prints without trailing decimals if it is an integer representation (e.g., `5`), or with explicit fractional scales if a true floating-point value is maintained (e.g., `5.25`).
- Strings: Extracted and printed directly to standard output without surrounding quotation marks.
- Nil: Prints explicitly as the lowercase textual literal `nil`.

### Truthiness
`SudoCode` follows a strict truthiness evaluation rule:
- Only the Boolean flag value `FALSE` and the empty object state `NIL` are interpreted as falsy conditions.
- Every other initialized value type, object, number, or non-empty string is resolved as **true**.

### Operator semantics
- Arithmetic: Requires both matching operands to be `NUMBER` types. Any alternative configuration throws a static error.
- `+` on strings: Performs string concatenation if either operand evaluates as a string literal (e.g., `"Value: " + 5` produces `"Value: 5"`).
- Mixed types: Triggers an operational type-mismatch error unless evaluated by string concatenation setups.
- Comparison: Allowed exclusively between numerical items.
- Equality across types: Comparing different types (e.g., matching a string to a number) resolves directly as `false` safely without triggering a system crash.
- Division by zero: Throws a runtime evaluation error with exit code `70`.

### Scope and bindings
- Redeclaration in the same scope: Attempting to `INITIALIZE` a variable that already exists in the same scope causes a static analysis error exiting under code `65`.
- Uninitialized variable holds: Cannot occur. Variables must explicitly pass through an `INITIALIZE` binding sequence, unless it is a counter variable such as in `FOR`.
- Shadowing: Inner local block levels fully shadow broader identifier declarations safely.
- Undefined name: Referencing unmapped variable names causes a static validation crash exiting with code `65`.

### Control flow and functions
- Logical operators return: The specific logical evaluated `bool` answer (`TRUE` or `FALSE`).
- Dangling else binds to: The nearest nested, unresolved, open `IF` statement block.
- Closure capture of a loop variable: Shared reference capture behavior across execution paths.
- Function with no return statement produces: A standard default state tracking value of `NIL`.
- Arity mismatch: Passing the wrong number of arguments to a function throws a static signature mismatch error exiting under code `65`. 

## Native functions
*To be defined in a later lab activity.*

## Errors and diagnostics
Message format:

```text
[Line 3] Lexical Error: Unexpected character '@' found in scan block.
[Line 5] Runtime Error: Division by zero is undefined.
```

| Failure | Exit code |
|---|---|
| lexical error | `65` |
| syntax error | `65` |
| runtime error | `70` |


## Testing conventions
| Folder | Activity | Mode | Flag |
|---|---|---|---|
| tests/lab1 | Scanner | sidecar | `--tokenize` |
| tests/lab2 | Parser | sidecar | `--parse` |
| tests/lab3 | Evaluator | inline | `--eval` |
| tests/lab4 | Context | inline | none |
| tests/lab5 | Functions | inline | none |

```
Lab 1 tests cover keywords and identifiers, operators, conditional and loop tokens, and lexical error handling.
```

Run locally with:
```bash
curl -sSL https://raw.githubusercontent.com/WhiteLicorice/cmsc-124-harness/v1.1/run_tests.py -o run_tests.py
./build.sh
python3 run_tests.py tests/lab1
```

## Sample code
```text
INITIALIZE counter <- 1
FOR i IN 1 TO 5 DO
    IF i MOD 2 = 0 THEN
        PRINT i
        SET counter <- counter + 1
    END IF
END FOR
```

Output:
```text
2
4
```

## Design rationale
`SudoCode` was intentionally designed to capture the structural clarity of classic academic pseudocode while eliminating C-style syntax clutter. We explicitly decoupled our keywords to follow simple, clean, individual token blocks (such as processing `END` and `IF` as separate tokens rather than a single compound lexeme), allowing our upcoming parser rules to safely establish scope logic. We chose to separate declaration (`INITIALIZE`) from mutation (`SET`) to enforce absolute clarity when reading state transformations. 

Our most significant mid-design pivot was removing the dual assignment meaning of the `TO` keyword. Originally conceptualized for assignments (e.g., `SET x TO 5`), this created a parsing ambiguity with traditional `FOR i IN 1 TO 10` iteration limits. We resolved this conflict by adopting the universal pseudocode assignment arrow (`<-`) and reserving `TO` strictly for loop boundaries. We also opted out of using standard brackets or curly braces, leaning entirely into matching text boundaries (like `IF` paired with `END IF`) to keep program code clean, flowing, and readable.

## Known limitations
- Standard multi-line block commenting structures are completely unsupported; documentation annotations are restricted strictly to single-line `>>` prefixes.
- Compact mutating expressions such as `++`, `--`, or `+=` do not exist, requiring explicit manual assignments (`SET x <- x + 1`).
- Escape code literals (such as `\n` or `\t`) inside text strings are currently treated as uninterpreted character sets rather than active formatting commands.

## Changelog
| Activity | What changed in the language |
|---|---|
| Lab 1 | Initial language specifications locked; custom operators, tokens, and pseudocode structures defined. |
