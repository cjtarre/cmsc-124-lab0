using System;
using System.IO;
using System.Text;

static void Fail(string message){
    Console.Error.WriteLine($"SudoCode: {message}");
    Environment.Exit(65);
}

static string? LoadSource(string path) {
    try {
        return File.ReadAllText(path, Encoding.UTF8);
    } catch (Exception error) when (error is IOException or UnauthorizedAccessException) {
        Fail($"cannot read '{path}': {error.Message}");
    } catch (Exception error) {
        Fail($"error while reading '{path}': {error.Message}");
    }
    return null;
}

static Scanner? Scan(string path) {
    string? source = LoadSource(path);
    if (source == null) return null;

    try {
        Scanner scanner = new Scanner(source);
        scanner.ScanTokens();
        return scanner;
    } catch (Exception error) {
        Fail($"error while scanning '{path}': {error.Message}");
        return null;
    }
}

static void REPL() {
    Console.WriteLine("SudoCode REPL 0.1.0 (Press Ctrl+C to exit)");
    while (true) {
        Console.Write("> ");
        string? line = Console.ReadLine();
        if (line == null) {
            break;
        }
        Scanner scanner = new Scanner(line);
        scanner.ScanTokens();
        scanner.PrintTokens(1);
    }
}

// Main program entry point
if (args.Length == 0) {
    REPL();
    return 0;
}
string path;
switch (args[0]) {
    case "--tokenize":
        if (args.Length != 2) Fail("expected --tokenize <source-file>");
        path = args[1];
        Scanner? scanner = Scan(path);
        scanner?.PrintTokens();
        break;

    case "--version":
        Console.WriteLine("SudoCode 0.1.0");
        break;
    default:
        if (args.Length != 1) {
            Fail("expected a source-file path");
        }
        path = args[0];
        try {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Write(File.ReadAllText(path, Encoding.UTF8));
            Environment.Exit(0);
        } catch (Exception error) when (error is IOException or UnauthorizedAccessException) {
            Fail($"cannot read '{path}': {error.Message}");
        } 
        break;
}
return 0;