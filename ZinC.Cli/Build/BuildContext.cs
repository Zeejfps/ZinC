using ZinC.Cli.Config;

namespace ZinC.Cli.Build;

internal sealed record BuildContext(
    ToolchainConfig Config,
    ModeConfig ModeConfig,
    PlatformConfig PlatformConfig,
    ArtifactTypeConfig ArtifactTypeConfig,
    string Mode,
    string Platform);
