using System.CommandLine;
using ZinC.Cli.Console;
using ZinC.Cli.Logging;

namespace ZinC.Cli.Setup;

internal sealed class SetupAction : ZincCommandAction
{
    public required Argument<ProjectType> ProjectTypeArgument { get; init; }
    public required Option<string> ArtifactNameOption { get; init; }
    public required Option<string> CompilerOption { get; init; }
    
    public override Argument[] Arguments => [ProjectTypeArgument];
    public override Option[] Options => [
        ArtifactNameOption, 
        CompilerOption
    ];

    public SetupAction(IConsole console, ILogger logger) : base(console, logger)
    {
    }

    protected override async Task<int> OnInvokedAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var projectType = parseResult.GetRequiredValue(ProjectTypeArgument);
        var artifactName = parseResult.GetValue(ArtifactNameOption) ?? "a";
        WriteLine("Project type: " + projectType + "");
        WriteLine("Artifact name: " + artifactName + "");
        return 0;
    }
}