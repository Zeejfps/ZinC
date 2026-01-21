using System.CommandLine;
using ZinC.Cli.Console;
using ZinC.Cli.Logging;

namespace ZinC.Cli.Build;

internal sealed class BuildAction : ZincCommandAction
{
    public required Option<string> ModeOption { get; init; }
    public required Option<string> PlatformOption { get; init; }
    public required Option<string> CompilerOption { get; init; }
    
    public BuildAction(IConsole console, ILogger logger) : base(console, logger)
    {
    }

    protected override Task<int> OnInvokedAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}