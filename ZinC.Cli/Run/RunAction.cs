using System.CommandLine;
using ZinC.Cli.Console;
using ZinC.Cli.Logging;

namespace ZinC.Cli.Run;

internal sealed class RunAction : ZincCommandAction
{
    public RunAction(IConsole console, ILogger logger) : base(console, logger)
    {
    }

    protected override Task<int> OnInvokedAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}