using ZinC.Cli.Config;

namespace ZinC.Cli.Build;

internal sealed record BuildContext(
    ProjectConfig ProjectConfig,
    ProjectPlatformConfig? ProjectPlatformConfig,
    ToolchainConfig ToolchainConfig,
    ModeConfig ModeConfig,
    PlatformConfig PlatformConfig,
    ArtifactTypeConfig ArtifactTypeConfig,
    string Mode,
    string Platform);
