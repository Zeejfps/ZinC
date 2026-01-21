namespace ZinC.Cli.Logging;

internal sealed class ConsoleLogger : ILogger
{
    public void LogException(Exception exception)
    {
        System.Console.Error.WriteLine(exception);
    }
}