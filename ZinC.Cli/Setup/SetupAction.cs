using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Nodes;
using ZinC.Cli.Console;
using ZinC.Cli.Logging;
using ZinC.Cli.Resources;

namespace ZinC.Cli.Setup;

internal sealed class SetupAction : ZincCommandAction
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly EmbeddedResourcesService _resources = new();

    public required Argument<ProjectType> ProjectTypeArgument { get; init; }
    public required Option<string> ArtifactNameOption { get; init; }
    public required Option<string> CompilerOption { get; init; }

    public override Argument[] Arguments => [ProjectTypeArgument];
    public override Option[] Options =>
    [
        ArtifactNameOption,
        CompilerOption
    ];

    public SetupAction(IConsole console, ILogger logger) : base(console, logger)
    {
    }

    protected override async Task<int> OnInvokedAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var projectType = parseResult.GetRequiredValue(ProjectTypeArgument);
        var artifactName = parseResult.GetValue(ArtifactNameOption);
        var compiler = parseResult.GetValue(CompilerOption) ?? "gcc";

        // Load the compiler config from embedded resources
        var configJson = await _resources.ReadResourceAsStringAsync($"{compiler}.json", cancellationToken);
        if (configJson is null)
        {
            WriteErrorLine($"Unknown compiler: {compiler}");
            WriteErrorLine($"Available compilers: {string.Join(", ", _resources.ListResources().Select(Path.GetFileNameWithoutExtension))}");
            return 1;
        }

        var config = JsonNode.Parse(configJson)!.AsObject();

        // Update artifact name if provided
        if (artifactName is not null)
        {
            config["artifact_name"] = artifactName;
        }

        // Update artifact type based on project type
        config["artifact_type"] = projectType switch
        {
            ProjectType.Executable => "executable",
            ProjectType.StaticLibrary => "static_library",
            ProjectType.DynamicLibrary => "shared_library",
            _ => throw new ArgumentOutOfRangeException(nameof(projectType))
        };

        // Get directory paths from config
        var srcDir = config["src_dir"]?.GetValue<string>() ?? "src";
        var outDir = config["out_dir"]?.GetValue<string>() ?? "out";
        var includeDirs = config["include_dirs"]?.AsArray()
            .Select(n => n!.GetValue<string>())
            .ToArray() ?? ["include"];

        // Create directories
        Directory.CreateDirectory(srcDir);
        Directory.CreateDirectory(outDir);
        foreach (var includeDir in includeDirs)
        {
            Directory.CreateDirectory(includeDir);
        }

        // Write the config file
        var outputJson = config.ToJsonString(JsonOptions);
        await File.WriteAllTextAsync($"{compiler}.json", outputJson, cancellationToken);

        WriteLine($"Created project with {compiler} compiler");
        WriteLine($"  Type: {projectType}");
        WriteLine($"  Config: {compiler}.json");
        WriteLine($"  Source: {srcDir}/");
        WriteLine($"  Output: {outDir}/");
        foreach (var includeDir in includeDirs)
        {
            WriteLine($"  Include: {includeDir}/");
        }

        return 0;
    }
}