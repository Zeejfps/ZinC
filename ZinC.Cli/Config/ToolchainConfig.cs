using System.Text.Json.Serialization;

namespace ZinC.Cli.Config;

internal sealed record ToolchainConfig
{
    [JsonPropertyName("compiler_exe")]
    public string? CompilerExe { get; init; }

    [JsonPropertyName("lib_dir_format")]
    public string? LibDirFormat { get; init; }

    [JsonPropertyName("lib_format")]
    public string? LibFormat { get; init; }

    [JsonPropertyName("compile_flag")]
    public string? CompileFlag { get; init; }

    [JsonPropertyName("compile_output_format")]
    public string? CompileOutputFormat { get; init; }

    [JsonPropertyName("link_output_format")]
    public string? LinkOutputFormat { get; init; }

    [JsonPropertyName("flags")]
    public List<string>? Flags { get; init; }

    [JsonPropertyName("link_flags")]
    public List<string>? LinkFlags { get; init; }

    [JsonPropertyName("defines")]
    public List<string>? Defines { get; init; }

    [JsonPropertyName("modes")]
    public Dictionary<string, ModeConfig>? Modes { get; init; }

    [JsonPropertyName("platforms")]
    public Dictionary<string, PlatformConfig>? Platforms { get; init; }

    [JsonPropertyName("artifact_types")]
    public Dictionary<string, ArtifactTypeConfig>? ArtifactTypes { get; init; }
}

internal sealed record ModeConfig
{
    [JsonPropertyName("flags")]
    public List<string>? Flags { get; init; }

    [JsonPropertyName("defines")]
    public List<string>? Defines { get; init; }

    [JsonPropertyName("link_flags")]
    public List<string>? LinkFlags { get; init; }
}

internal sealed record PlatformConfig
{
    [JsonPropertyName("artifact_extension")]
    public string? ArtifactExtension { get; init; }

    [JsonPropertyName("static_lib_extension")]
    public string? StaticLibExtension { get; init; }

    [JsonPropertyName("shared_lib_extension")]
    public string? SharedLibExtension { get; init; }

    [JsonPropertyName("object_extension")]
    public string? ObjectExtension { get; init; }

    [JsonPropertyName("defines")]
    public List<string>? Defines { get; init; }

    [JsonPropertyName("flags")]
    public List<string>? Flags { get; init; }

    [JsonPropertyName("libs")]
    public List<string>? Libs { get; init; }

    [JsonPropertyName("lib_dirs")]
    public List<string>? LibDirs { get; init; }

    [JsonPropertyName("link_flags")]
    public List<string>? LinkFlags { get; init; }
}

internal sealed record ArtifactTypeConfig
{
    [JsonPropertyName("flags")]
    public List<string>? Flags { get; init; }

    [JsonPropertyName("link_flags")]
    public List<string>? LinkFlags { get; init; }

    [JsonPropertyName("use_archiver")]
    public bool UseArchiver { get; init; }

    [JsonPropertyName("archiver_flags")]
    public List<string>? ArchiverFlags { get; init; }
}
