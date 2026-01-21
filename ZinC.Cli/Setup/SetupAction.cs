using System.CommandLine;
using ZinC.Cli.Console;
using ZinC.Cli.Logging;

namespace ZinC.Cli.Setup;

internal sealed class SetupAction : ZincCommandAction
{
    public required Argument<ProjectType> ProjectTypeArgument { get; init; }

    public override Argument[] Arguments => [ProjectTypeArgument];

    public SetupAction(IConsole console, ILogger logger) : base(console, logger)
    {
    }

    protected override Task<int> OnInvokedAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}