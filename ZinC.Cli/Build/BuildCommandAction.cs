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

        var missingProperties = ValidateRequiredProperties(config);
        if (missingProperties.Count > 0)
        {
            WriteErrorLine($"Config is missing required properties: {string.Join(", ", missingProperties)}");
            return 1;
        }

        var modes = config.Modes ?? [];
        if (!modes.TryGetValue(mode, out var modeConfig))
        {
            WriteErrorLine($"Unknown mode: {mode}. Available: {string.Join(", ", modes.Keys)}");
            return 1;
        }

        var platforms = config.Platforms ?? [];
        if (!platforms.TryGetValue(platform, out var platformConfig))
        {
            WriteErrorLine($"Unknown platform: {platform}. Available: {string.Join(", ", platforms.Keys)}");
            return 1;
        }

        var artifactTypes = config.ArtifactTypes ?? [];
        var artifactType = config.ArtifactType ?? "executable";
        if (!artifactTypes.TryGetValue(artifactType, out var artifactTypeConfig))
        {
            WriteErrorLine($"Unknown artifact type: {artifactType}. Available: {string.Join(", ", artifactTypes.Keys)}");
            return 1;
        }

        var context = new BuildContext(config, modeConfig, platformConfig, artifactTypeConfig, mode, platform);
        return await ExecuteAsync(context, cancellationToken);
    }

    protected abstract Task<int> ExecuteAsync(BuildContext context, CancellationToken cancellationToken);

    private static List<string> ValidateRequiredProperties(ToolchainConfig config)
    {
        var missing = new List<string>();

        if (string.IsNullOrEmpty(config.CompilerExe))
            missing.Add("compiler_exe");
        if (string.IsNullOrEmpty(config.CompileFlag))
            missing.Add("compile_flag");
        if (string.IsNullOrEmpty(config.CompileOutputFormat))
            missing.Add("compile_output_format");
        if (string.IsNullOrEmpty(config.LinkOutputFormat))
            missing.Add("link_output_format");
        if (string.IsNullOrEmpty(config.LibDirFormat))
            missing.Add("lib_dir_format");
        if (string.IsNullOrEmpty(config.LibFormat))
            missing.Add("lib_format");

        return missing;
    }
}
