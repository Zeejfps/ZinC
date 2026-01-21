using System.CommandLine;
using ZinC.Cli.Console;
using ZinC.Cli.Logging;

namespace ZinC.Cli.Build;

internal sealed class BuildAction : ZincCommandAction
{
    public BuildAction(IConsole console, ILogger logger) : base(console, logger)
    {
    }

    protected override Task<int> OnInvokedAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}