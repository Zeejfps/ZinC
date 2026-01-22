using ZinC.Cli.Config;
using ZinC.Cli.Toolchains;

namespace ZinC.Cli.Build;

internal sealed class BuildContextFactory
{
    public async Task<BuildContextResult> CreateContextAsync(
        string toolchainName,
        string platform,
        string mode,
        CancellationToken cancellationToken = default)
    {
        // Load project config from zinc.json
        var projectConfigService = new ProjectConfigService();
        var projectConfig = await projectConfigService.LoadAsync(cancellationToken: cancellationToken);
        if (projectConfig is null)
        {
            return new BuildContextResult("Project config not found: zinc.json");
        }

        // Load toolchain config (file first, then embedded fallback)
        var toolchainConfigService = new ToolchainConfigService();
        var toolchainConfig = await toolchainConfigService.LoadAsync(toolchainName, cancellationToken: cancellationToken);
        if (toolchainConfig is null)
        {
            var available = string.Join(", ", toolchainConfigService.ListEmbeddedToolchains());
            return new BuildContextResult($"Toolchain not found: {toolchainName}. Available embedded: {available}");
        }

        var missingProperties = ValidateRequiredProperties(toolchainConfig);
        if (missingProperties.Count > 0)
        {
            return new BuildContextResult($"Toolchain config is missing required properties: {string.Join(", ", missingProperties)}");
        }

        var modes = toolchainConfig.Modes ?? [];
        if (!modes.TryGetValue(mode, out var modeConfig))
        {
            return new BuildContextResult($"Unknown mode: {mode}. Available: {string.Join(", ", modes.Keys)}");
        }

        var platforms = toolchainConfig.Platforms ?? [];
        if (!platforms.TryGetValue(platform, out var platformConfig))
        {
            return new BuildContextResult($"Unknown platform: {platform}. Available: {string.Join(", ", platforms.Keys)}");
        }

        var artifactTypes = toolchainConfig.ArtifactTypes ?? [];
        var artifactType = projectConfig.ArtifactType ?? "executable";
        if (!artifactTypes.TryGetValue(artifactType, out var artifactTypeConfig))
        {
            return new BuildContextResult($"Unknown artifact type: {artifactType}. Available: {string.Join(", ", artifactTypes.Keys)}");
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

        return new BuildContextResult(context);
    }

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

internal sealed class BuildContextResult
{
    public BuildContext? Context { get; }
    public string? ErrorMessage { get; }
    public bool IsSuccess => ErrorMessage is null;

    public BuildContextResult(BuildContext context)
    {
        Context = context;
    }

    public BuildContextResult(string errorMessage)
    {
        ErrorMessage = errorMessage;
    }
}
