using System.Text.Json;

namespace ZinC.Cli.Config;

internal sealed class ToolchainConfigService
{
    private const string DefaultConfigFileName = "zinc.json";

    public ToolchainConfig? Load(string? configPath = null, string? workingDirectory = null)
    {
        var path = ResolveConfigPath(configPath, workingDirectory);

        if (!File.Exists(path))
        {
            return null;
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize(json, ToolchainConfigJsonContext.Default.ToolchainConfig);
    }

    public async Task<ToolchainConfig?> LoadAsync(string? configPath = null, string? workingDirectory = null, CancellationToken cancellationToken = default)
    {
        var path = ResolveConfigPath(configPath, workingDirectory);

        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync(stream, ToolchainConfigJsonContext.Default.ToolchainConfig, cancellationToken);
    }

    public void Save(ToolchainConfig config, string? configPath = null, string? workingDirectory = null)
    {
        var path = ResolveConfigPath(configPath, workingDirectory);
        var json = JsonSerializer.Serialize(config, ToolchainConfigJsonContext.Default.ToolchainConfig);
        File.WriteAllText(path, json);
    }

    public async Task SaveAsync(ToolchainConfig config, string? configPath = null, string? workingDirectory = null, CancellationToken cancellationToken = default)
    {
        var path = ResolveConfigPath(configPath, workingDirectory);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, config, ToolchainConfigJsonContext.Default.ToolchainConfig, cancellationToken);
    }

    private static string ResolveConfigPath(string? configPath, string? workingDirectory)
    {
        if (!string.IsNullOrEmpty(configPath))
        {
            return Path.IsPathRooted(configPath)
                ? configPath
                : Path.Combine(workingDirectory ?? Directory.GetCurrentDirectory(), configPath);
        }

        return Path.Combine(workingDirectory ?? Directory.GetCurrentDirectory(), DefaultConfigFileName);
    }
}