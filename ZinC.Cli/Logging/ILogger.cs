namespace ZinC.Cli.Logging;

internal interface ILogger
{
    void LogException(Exception exception);
}