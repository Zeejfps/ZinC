namespace ZinC.Cli;

internal interface IConsole
{
    void WriteLine(string message);
    void WriteErrorLine(string message);
}