using System;
using System.IO;
using System.Text;

static int Fail(string message)
{
    Console.Error.WriteLine($"SudoCode: {message}");
    return 65;
}

static int LoadFile(string path) {
    try {
        string content = File.ReadAllText(path, Encoding.UTF8);
        Scanner scanner = new Scanner(content);
        scanner.ScanTokens();
        return 0;
    } catch (Exception error) when (error is IOException or UnauthorizedAccessException) {
        return Fail($"cannot read '{path}': {error.Message}");
    } catch (Exception error) {
        return Fail($"error while scanning '{path}': {error.Message}");
    }
}

static int REPL() {
    Console.WriteLine("SudoCode REPL 0.1.0 (Press Ctrl+C to exit)");
    while (true) {
        Console.Write("> ");
        string? line = Console.ReadLine();
        if (line == null) {
            break;
        }
        Scanner scanner = new Scanner(line);
        scanner.ScanTokens();
    }
    return 0;
}

// Main program entry point
if (args.Length == 0) {
    return REPL();
}
string path;
switch (args[0]) {
    case "--tokenize":
        if (args.Length != 2) {
            return Fail("expected --tokenize <source-file>");
        }
        path = args[1];
        return LoadFile(path);

    case "--version":
        Console.WriteLine("SudoCode 0.1.0");
        return 0;
    default:
        if (args.Length != 1) {
            return Fail("expected a source-file path");
        }
        path = args[0];
        try {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Write(File.ReadAllText(path, Encoding.UTF8));
            return 0;
        } catch (Exception error) when (error is IOException or UnauthorizedAccessException) {
            return Fail($"cannot read '{path}': {error.Message}");
        }
}