using System.Text.Json.Serialization;

namespace ZinC.Cli.Config;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(ToolchainConfig))]
internal sealed partial class ToolchainConfigJsonContext : JsonSerializerContext;
