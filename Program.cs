using System.Text;

static int Fail(string message)
{
    Console.Error.WriteLine($"SudoCode: {message}");
    return 65;
}

if (args.Length == 0)
{
    return Fail("expected a source-file path or --tokenize <source-file>");
}

string path;

if (args[0] == "--tokenize")
{
    if (args.Length != 2)
    {
        return Fail("expected --tokenize <source-file>");
    }

    path = args[1];
}
else
{
    if (args.Length != 1)
    {
        return Fail("expected a source-file path");
    }

    path = args[0];
}

try
{
    Console.OutputEncoding = Encoding.UTF8;
    Console.Write(File.ReadAllText(path, Encoding.UTF8));
    return 0;
}
catch (Exception error) when (error is IOException or UnauthorizedAccessException)
{
    return Fail($"cannot read '{path}': {error.Message}");
}