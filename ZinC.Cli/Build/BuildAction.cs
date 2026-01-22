using System.CommandLine;
using System.Diagnostics;
using ZinC.Cli.Config;
using ZinC.Cli.Console;
using ZinC.Cli.Logging;
using ZinC.Cli.Toolchains;

namespace ZinC.Cli.Build;

internal sealed class BuildAction : ZincCommandAction
{
    public required Option<string> ModeOption { get; init; }
    public required Option<string> PlatformOption { get; init; }
    public required Option<string> ToolchainOption { get; init; }
    public required Option<bool> RunOption { get; init; }

    public override Option[] Options => [ModeOption, PlatformOption, ToolchainOption, RunOption];

    private readonly IConsole _console;

    public BuildAction(IConsole console, ILogger logger) : base(console, logger)
    {
        _console = console;
    }

    protected override async Task<int> OnInvokedAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var mode = parseResult.GetRequiredValue(ModeOption);
        var platform = parseResult.GetRequiredValue(PlatformOption);
        var toolchainName = parseResult.GetRequiredValue(ToolchainOption);
        var shouldRun = parseResult.GetValue(RunOption);

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

        // Build
        var artifactName = projectConfig.ArtifactName ?? "a";
        WriteLine($"Building {artifactName} ({mode}/{platform})");

        var buildService = new BuildService(_console);
        var result = await buildService.BuildAsync(context, cancellationToken: cancellationToken);

        if (!result.Success || result.ArtifactPath is null || !shouldRun)
        {
            return result.ExitCode;
        }

        // Run
        if (artifactType != "executable")
        {
            WriteErrorLine($"Cannot run artifact type: {artifactType}. Only executables can be run.");
            return 1;
        }

        WriteLine("");
        WriteLine($"Running {Path.GetFileName(result.ArtifactPath)}...");
        WriteLine("");

        return await RunArtifactAsync(result.ArtifactPath, cancellationToken);
    }

    private async Task<int> RunArtifactAsync(string artifactPath, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = artifactPath,
            WorkingDirectory = Path.GetDirectoryName(artifactPath),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process();
        process.StartInfo = startInfo;
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null) WriteLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null) WriteErrorLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        WriteLine("");
        WriteLine($"Process exited with code {process.ExitCode}");

        return process.ExitCode;
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
