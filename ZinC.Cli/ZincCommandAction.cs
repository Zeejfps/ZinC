using System.CommandLine;
using System.CommandLine.Invocation;
using ZinC.Cli.Console;
using ZinC.Cli.Logging;

namespace ZinC.Cli;

internal abstract class ZincCommandAction : AsynchronousCommandLineAction
{
    public virtual Argument[] Arguments => [];
    public virtual Option[] Options => [];
    
    private readonly IConsole _console;
    private readonly ILogger _logger;

    protected ZincCommandAction(IConsole console, ILogger logger)
    {
        _console = console;
        _logger = logger;
    }

    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        try
        {
            return await OnInvokedAsync(parseResult, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        catch (Exception ex)
        {
            WriteErrorLine($"Failed to execute {parseResult.CommandResult.Command} command");
            WriteErrorLine($"{ex.GetType()}: {ex.Message}");
            _logger.LogException(ex);
            return 1;
        }
    }
    
    protected abstract Task<int> OnInvokedAsync(ParseResult parseResult, CancellationToken cancellationToken);

    protected void WriteLine(string message)
    {
        _console.WriteLine(message);
    }

    protected void WriteErrorLine(string message)
    {
        _console.WriteErrorLine(message);
    }
}