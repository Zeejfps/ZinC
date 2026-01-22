using System.Text.Json.Serialization;

namespace ZinC.Cli.Build;

internal sealed record CompileCommand
{
    [JsonPropertyName("directory")]
    public required string Directory { get; init; }

    [JsonPropertyName("arguments")]
    public required List<string> Arguments { get; init; }

    [JsonPropertyName("file")]
    public required string File { get; init; }
}

[JsonSerializable(typeof(List<CompileCommand>))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal sealed partial class CompileCommandJsonContext : JsonSerializerContext;
