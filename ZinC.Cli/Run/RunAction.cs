using System.Diagnostics;
using ZinC.Cli.Build;
using ZinC.Cli.Console;
using ZinC.Cli.Logging;

namespace ZinC.Cli.Run;

internal sealed class RunAction : BuildCommandAction
{
    public RunAction(IConsole console, ILogger logger) : base(console, logger)
    {
    }

    protected override async Task<int> ExecuteAsync(BuildContext context, CancellationToken cancellationToken)
    {
        if (context.Config.ArtifactType != "executable")
        {
            WriteErrorLine($"Cannot run artifact type: {context.Config.ArtifactType}. Only executables can be run.");
            return 1;
        }

        WriteLine($"Building {context.Config.ArtifactName} ({context.Mode}/{context.Platform})");

        var buildService = new BuildService(Console);
        var buildResult = await buildService.BuildAsync(
            context.Config,
            context.ModeConfig,
            context.PlatformConfig,
            context.ArtifactTypeConfig,
            cancellationToken: cancellationToken);

        if (!buildResult.Success || buildResult.ArtifactPath is null)
        {
            return buildResult.ExitCode;
        }

        WriteLine("");
        WriteLine($"Running {Path.GetFileName(buildResult.ArtifactPath)}...");
        WriteLine("");

        return await RunArtifactAsync(buildResult.ArtifactPath, cancellationToken);
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
