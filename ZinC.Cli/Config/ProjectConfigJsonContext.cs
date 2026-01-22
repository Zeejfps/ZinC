using System.Text.Json.Serialization;

namespace ZinC.Cli.Config;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(ProjectConfig))]
internal sealed partial class ProjectConfigJsonContext : JsonSerializerContext;
