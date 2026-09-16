using System;
using System.IO;
using System.Text;

static void Fail(string message){
    Console.Error.WriteLine($"SudoCode: {message}");
    Environment.Exit(65);
}

static void LoadFile(string path) {
    try {
        string content = File.ReadAllText(path, Encoding.UTF8);
        Scanner scanner = new Scanner(content);
        scanner.ScanTokens();
        scanner.PrintTokens();
    } catch (Exception error) when (error is IOException or UnauthorizedAccessException) {
        Fail($"cannot read '{path}': {error.Message}");
    } catch (Exception error) {
        Fail($"error while scanning '{path}': {error.Message}");
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
        scanner.PrintTokens(1);
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
            Fail("expected --tokenize <source-file>");
        }
        path = args[1];
        LoadFile(path);
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