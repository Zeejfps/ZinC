using System.CommandLine;
using ZinC.Cli.Console;
using ZinC.Cli.Logging;

namespace ZinC.Cli.Run;

internal sealed class RunAction : ZincCommandAction
{
    public required Option<string> ModeOption { get; init; }
    public required Option<string> PlatformOption { get; init; }
    public required Option<string> ConfigOption { get; init; }

    public override Option[] Options => [ModeOption, PlatformOption, ConfigOption];

    
    public RunAction(IConsole console, ILogger logger) : base(console, logger)
    {
    }

    protected override Task<int> OnInvokedAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}