using System.CommandLine;
using ZinC.Cli.Config;
using ZinC.Cli.Console;
using ZinC.Cli.Logging;

namespace ZinC.Cli.Build;

internal sealed class BuildAction : ZincCommandAction
{
    public required Option<string> ModeOption { get; init; }
    public required Option<string> PlatformOption { get; init; }
    public required Option<string> ConfigOption { get; init; }

    public override Option[] Options => [ModeOption, PlatformOption, ConfigOption];

    private readonly IConsole _console;

    public BuildAction(IConsole console, ILogger logger) : base(console, logger)
    {
        _console = console;
    }

    protected override async Task<int> OnInvokedAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var mode = parseResult.GetRequiredValue(ModeOption);
        var platform = parseResult.GetRequiredValue(PlatformOption);
        var configPath = parseResult.GetRequiredValue(ConfigOption);

        if (!configPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            configPath += ".json";
        }

        var configService = new ToolchainConfigService();
        var config = await configService.LoadAsync(configPath, cancellationToken: cancellationToken);
        if (config is null)
        {
            WriteErrorLine($"Config file not found: {configPath}");
            return 1;
        }

        if (!config.Modes.TryGetValue(mode, out var modeConfig))
        {
            WriteErrorLine($"Unknown mode: {mode}. Available: {string.Join(", ", config.Modes.Keys)}");
            return 1;
        }

        if (!config.Platforms.TryGetValue(platform, out var platformConfig))
        {
            WriteErrorLine($"Unknown platform: {platform}. Available: {string.Join(", ", config.Platforms.Keys)}");
            return 1;
        }

        if (!config.ArtifactTypes.TryGetValue(config.ArtifactType, out var artifactTypeConfig))
        {
            WriteErrorLine($"Unknown artifact type: {config.ArtifactType}. Available: {string.Join(", ", config.ArtifactTypes.Keys)}");
            return 1;
        }

        WriteLine($"Building {config.ArtifactName} ({mode}/{platform})");

        var buildService = new BuildService(_console);
        var result = await buildService.BuildAsync(config, modeConfig, platformConfig, artifactTypeConfig, cancellationToken: cancellationToken);

        return result.ExitCode;
    }
}
