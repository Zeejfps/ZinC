namespace ZinC.Cli.Build;

internal sealed record BuildResult(bool Success, string? ArtifactPath, int ExitCode);
