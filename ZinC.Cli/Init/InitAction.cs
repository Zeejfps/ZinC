using System.CommandLine;
using ZinC.Cli.Console;
using ZinC.Cli.Logging;

namespace ZinC.Cli.Init;

internal sealed class InitAction : ZincCommandAction
{
    public InitAction(IConsole console, ILogger logger) : base(console, logger)
    {
    }

    protected override Task<int> OnInvokedAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}