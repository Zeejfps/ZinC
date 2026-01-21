using System.CommandLine;
using ZinC.Cli.Config;
using ZinC.Cli.Console;
using ZinC.Cli.Logging;

namespace ZinC.Cli.Build;

internal abstract class BuildCommandAction : ZincCommandAction
{
    public required Option<string> ModeOption { get; init; }
    public required Option<string> PlatformOption { get; init; }
    public required Option<string> ConfigOption { get; init; }

    public override Option[] Options => [ModeOption, PlatformOption, ConfigOption];

    protected IConsole Console { get; }

    protected BuildCommandAction(IConsole console, ILogger logger) : base(console, logger)
    {
        Console = console;
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

        var context = new BuildContext(config, modeConfig, platformConfig, artifactTypeConfig, mode, platform);
        return await ExecuteAsync(context, cancellationToken);
    }

    protected abstract Task<int> ExecuteAsync(BuildContext context, CancellationToken cancellationToken);
}
