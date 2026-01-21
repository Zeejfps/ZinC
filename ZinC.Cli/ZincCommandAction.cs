using System.CommandLine;
using System.CommandLine.Invocation;

namespace ZinC.Cli;

internal abstract class ZincCommandAction : AsynchronousCommandLineAction
{
    public virtual Argument[] Arguments => [];
    public virtual Option[] Options => [];

    private readonly IConsole _console;

    protected ZincCommandAction(IConsole console)
    {
        _console = console;
    }

    public override Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        return OnInvokedAsync(parseResult, cancellationToken);
    }
    
    protected abstract Task<int> OnInvokedAsync(ParseResult parseResult, CancellationToken cancellationToken);

    protected void WriteLine(string message)
    {
        _console.WriteLine(message);
    }
}