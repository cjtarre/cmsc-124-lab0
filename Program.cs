using System.Text;

static int Fail(string message)
{
    Console.Error.WriteLine($"lab0: {message}");
    return 65;
}

if (args.Length == 0)
{
    return Fail("expected one source-file path");
}

var path = args[0];

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