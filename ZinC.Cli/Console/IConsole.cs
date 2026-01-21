namespace ZinC.Cli.Console;

internal interface IConsole
{
    void WriteLine(string message);
    void WriteErrorLine(string message);
}