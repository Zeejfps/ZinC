using System.CommandLine;
using ZinC.Cli.Config;
using ZinC.Cli.Console;
using ZinC.Cli.Logging;
using ZinC.Cli.Toolchains;

namespace ZinC.Cli.Build;

internal abstract class BuildCommandAction : ZincCommandAction
{
    public required Option<string> ModeOption { get; init; }
    public required Option<string> PlatformOption { get; init; }
    public required Option<string> ToolchainOption { get; init; }

    public override Option[] Options => [ModeOption, PlatformOption, ToolchainOption];

    protected IConsole Console { get; }

    protected BuildCommandAction(IConsole console, ILogger logger) : base(console, logger)
    {
        Console = console;
    }

    protected override async Task<int> OnInvokedAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var mode = parseResult.GetRequiredValue(ModeOption);
        var platform = parseResult.GetRequiredValue(PlatformOption);
        var toolchainName = parseResult.GetRequiredValue(ToolchainOption);

        // Load project config from zinc.json
        var projectConfigService = new ProjectConfigService();
        var projectConfig = await projectConfigService.LoadAsync(cancellationToken: cancellationToken);
        if (projectConfig is null)
        {
            WriteErrorLine("Project config not found: zinc.json");
            return 1;
        }

        // Load toolchain config (file first, then embedded fallback)
        var toolchainConfigService = new ToolchainConfigService();
        var toolchainConfig = await toolchainConfigService.LoadAsync(toolchainName, cancellationToken: cancellationToken);
        if (toolchainConfig is null)
        {
            var available = string.Join(", ", toolchainConfigService.ListEmbeddedToolchains());
            WriteErrorLine($"Toolchain not found: {toolchainName}. Available embedded: {available}");
            return 1;
        }

        var missingProperties = ValidateRequiredProperties(toolchainConfig);
        if (missingProperties.Count > 0)
        {
            WriteErrorLine($"Toolchain config is missing required properties: {string.Join(", ", missingProperties)}");
            return 1;
        }

        var modes = toolchainConfig.Modes ?? [];
        if (!modes.TryGetValue(mode, out var modeConfig))
        {
            WriteErrorLine($"Unknown mode: {mode}. Available: {string.Join(", ", modes.Keys)}");
            return 1;
        }

        var platforms = toolchainConfig.Platforms ?? [];
        if (!platforms.TryGetValue(platform, out var platformConfig))
        {
            WriteErrorLine($"Unknown platform: {platform}. Available: {string.Join(", ", platforms.Keys)}");
            return 1;
        }

        var artifactTypes = toolchainConfig.ArtifactTypes ?? [];
        var artifactType = projectConfig.ArtifactType ?? "executable";
        if (!artifactTypes.TryGetValue(artifactType, out var artifactTypeConfig))
        {
            WriteErrorLine($"Unknown artifact type: {artifactType}. Available: {string.Join(", ", artifactTypes.Keys)}");
            return 1;
        }

        // Get platform-specific project config if available
        ProjectPlatformConfig? projectPlatformConfig = null;
        projectConfig.Platforms?.TryGetValue(platform, out projectPlatformConfig);

        var context = new BuildContext(
            projectConfig,
            projectPlatformConfig,
            toolchainConfig,
            modeConfig,
            platformConfig,
            artifactTypeConfig,
            mode,
            platform);

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
