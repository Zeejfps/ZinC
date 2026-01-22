using System.CommandLine;
using System.Diagnostics;
using ZinC.Cli.Console;
using ZinC.Cli.Logging;

namespace ZinC.Cli.Build;

internal sealed class BuildAction : ZincCommandAction
{
    public required Argument<string> ToolchainArgument { get; init; }
    public required Argument<string> PlatformArgument { get; init; }
    public required Argument<string> ModeArgument { get; init; }
    public required Option<bool> RunOption { get; init; }
    public required Option<bool> CompileCommandsOption { get; init; }
    public required Option<bool> VerboseOption { get; init; }

    public override Argument[] Arguments => [ToolchainArgument, PlatformArgument, ModeArgument];
    public override Option[] Options => [RunOption, CompileCommandsOption, VerboseOption];

    private readonly IConsole _console;

    public BuildAction(IConsole console, ILogger logger) : base(console, logger)
    {
        _console = console;
    }

    protected override async Task<int> OnInvokedAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var toolchainName = parseResult.GetRequiredValue(ToolchainArgument);
        var platform = parseResult.GetRequiredValue(PlatformArgument);
        var mode = parseResult.GetRequiredValue(ModeArgument);
        var shouldRun = parseResult.GetValue(RunOption);
        var generateCompileCommands = parseResult.GetValue(CompileCommandsOption);
        var verbose = parseResult.GetValue(VerboseOption);

        // Create build context
        var factory = new BuildContextFactory();
        var contextResult = await factory.CreateContextAsync(toolchainName, platform, mode, cancellationToken);

        if (!contextResult.IsSuccess)
        {
            WriteErrorLine(contextResult.ErrorMessage!);
            return 1;
        }

        var context = contextResult.Context!;
        var artifactName = context.ProjectConfig.ArtifactName ?? "a";
        var artifactType = context.ProjectConfig.ArtifactType ?? "executable";

        // Build
        WriteLine($"Building {artifactName} ({mode}/{platform})");

        var buildService = new BuildService(_console);
        var result = await buildService.BuildAsync(context, generateCompileCommands, verbose, cancellationToken: cancellationToken);

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
}
