using System.Text.Json.Serialization;

namespace ZinC.Cli.Config;

internal sealed record ProjectConfig
{
    [JsonPropertyName("artifact_name")]
    public string? ArtifactName { get; init; }

    [JsonPropertyName("artifact_type")]
    public string? ArtifactType { get; init; }

    [JsonPropertyName("src_dir")]
    public string? SrcDir { get; init; }

    [JsonPropertyName("out_dir")]
    public string? OutDir { get; init; }

    [JsonPropertyName("include_dirs")]
    public List<string>? IncludeDirs { get; init; }

    [JsonPropertyName("libs")]
    public List<string>? Libs { get; init; }

    [JsonPropertyName("lib_dirs")]
    public List<string>? LibDirs { get; init; }

    [JsonPropertyName("defines")]
    public List<string>? Defines { get; init; }

    [JsonPropertyName("platforms")]
    public Dictionary<string, ProjectPlatformConfig>? Platforms { get; init; }
}

internal sealed record ProjectPlatformConfig
{
    [JsonPropertyName("sources")]
    public List<string>? Sources { get; init; }

    [JsonPropertyName("include_dirs")]
    public List<string>? IncludeDirs { get; init; }

    [JsonPropertyName("libs")]
    public List<string>? Libs { get; init; }

    [JsonPropertyName("lib_dirs")]
    public List<string>? LibDirs { get; init; }

    [JsonPropertyName("defines")]
    public List<string>? Defines { get; init; }
}
