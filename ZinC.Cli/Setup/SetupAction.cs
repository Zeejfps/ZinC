using System.CommandLine;
using ZinC.Cli.Config;
using ZinC.Cli.Console;
using ZinC.Cli.Logging;

namespace ZinC.Cli.Setup;

internal sealed class SetupAction : ZincCommandAction
{
    public required Argument<ProjectType> ProjectTypeArgument { get; init; }
    public required Option<string> ArtifactNameOption { get; init; }

    public override Argument[] Arguments => [ProjectTypeArgument];
    public override Option[] Options => [ArtifactNameOption];

    public SetupAction(IConsole console, ILogger logger) : base(console, logger) { }

    protected override async Task<int> OnInvokedAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var projectType = parseResult.GetRequiredValue(ProjectTypeArgument);
        var artifactName = parseResult.GetValue(ArtifactNameOption);

        // Check if project config already exists
        var configPath = Path.Combine(Directory.GetCurrentDirectory(), "zinc.json");
        if (File.Exists(configPath))
        {
            WriteErrorLine($"Project config already exists: zinc.json");
            return 1;
        }

        // Determine artifact type based on project type
        var artifactType = projectType switch
        {
            ProjectType.Executable => "executable",
            ProjectType.StaticLibrary => "static_library",
            ProjectType.DynamicLibrary => "shared_library",
            _ => throw new ArgumentOutOfRangeException(nameof(projectType))
        };

        // Create project config
        var projectConfig = new ProjectConfig
        {
            ArtifactName = artifactName,
            ArtifactType = artifactType,
            SrcDir = "src",
            OutDir = "out",
            IncludeDirs = ["include"],
            Libs = [],
            LibDirs = [],
            Defines = []
        };

        // Create directories
        Directory.CreateDirectory("src");
        Directory.CreateDirectory("out");
        Directory.CreateDirectory("include");

        // Write project config
        var projectConfigService = new ProjectConfigService();
        await projectConfigService.SaveAsync(projectConfig, cancellationToken: cancellationToken);

        WriteLine($"Created project");
        WriteLine($"  Type: {artifactType}");
        if (artifactName is not null)
        {
            WriteLine($"  Name: {artifactName}");
        }
        WriteLine($"  Config: zinc.json");
        WriteLine($"  Source: src/");
        WriteLine($"  Include: include/");
        WriteLine($"  Output: out/");
        WriteLine("");
        WriteLine("To build: zinc build -m debug -p windows");
        WriteLine("To run:   zinc run -m debug -p windows");

        return 0;
    }
}