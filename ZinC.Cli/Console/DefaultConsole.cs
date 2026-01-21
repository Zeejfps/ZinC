namespace ZinC.Cli.Console;

internal sealed class DefaultConsole : IConsole
{
    public void WriteLine(string message)
    {
        System.Console.WriteLine(message);
    }

    public void WriteErrorLine(string message)
    {
        System.Console.Error.WriteLine(message);
    }
}