using ZinC.Cli.Console;
using ZinC.Cli.Logging;

namespace ZinC.Cli.Build;

internal sealed class BuildAction : BuildCommandAction
{
    public BuildAction(IConsole console, ILogger logger) : base(console, logger)
    {
    }

    protected override async Task<int> ExecuteAsync(BuildContext context, CancellationToken cancellationToken)
    {
        var artifactName = context.ProjectConfig.ArtifactName ?? "a";
        WriteLine($"Building {artifactName} ({context.Mode}/{context.Platform})");

        var buildService = new BuildService(Console);
        var result = await buildService.BuildAsync(context, cancellationToken: cancellationToken);

        return result.ExitCode;
    }
}
