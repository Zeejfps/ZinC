using System.Text.Json;
using ZinC.Cli.Resources;

namespace ZinC.Cli.Config;

internal sealed class ToolchainConfigService
{
    private readonly EmbeddedResourcesService _embeddedResources;

    public ToolchainConfigService()
    {
        _embeddedResources = new EmbeddedResourcesService();
    }

    public ToolchainConfig? Load(string toolchainName, string? workingDirectory = null)
    {
        var configPath = ResolveConfigPath(toolchainName, workingDirectory);

        // Try loading from file first
        if (File.Exists(configPath))
        {
            var json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize(json, ToolchainConfigJsonContext.Default.ToolchainConfig);
        }

        // Fall back to embedded resource
        return LoadFromEmbedded(toolchainName);
    }

    public async Task<ToolchainConfig?> LoadAsync(string toolchainName, string? workingDirectory = null, CancellationToken cancellationToken = default)
    {
        var configPath = ResolveConfigPath(toolchainName, workingDirectory);

        // Try loading from file first
        if (File.Exists(configPath))
        {
            await using var stream = File.OpenRead(configPath);
            return await JsonSerializer.DeserializeAsync(stream, ToolchainConfigJsonContext.Default.ToolchainConfig, cancellationToken);
        }

        // Fall back to embedded resource
        return await LoadFromEmbeddedAsync(toolchainName, cancellationToken);
    }

    public void Save(ToolchainConfig config, string toolchainName, string? workingDirectory = null)
    {
        var path = ResolveConfigPath(toolchainName, workingDirectory);
        var json = JsonSerializer.Serialize(config, ToolchainConfigJsonContext.Default.ToolchainConfig);
        File.WriteAllText(path, json);
    }

    public async Task SaveAsync(ToolchainConfig config, string toolchainName, string? workingDirectory = null, CancellationToken cancellationToken = default)
    {
        var path = ResolveConfigPath(toolchainName, workingDirectory);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, config, ToolchainConfigJsonContext.Default.ToolchainConfig, cancellationToken);
    }

    public ToolchainConfig? LoadFromEmbedded(string toolchainName)
    {
        var resourceName = $"{toolchainName}.json";
        var json = _embeddedResources.ReadResourceAsString(resourceName);
        if (json is null)
            return null;

        return JsonSerializer.Deserialize(json, ToolchainConfigJsonContext.Default.ToolchainConfig);
    }

    public async Task<ToolchainConfig?> LoadFromEmbeddedAsync(string toolchainName, CancellationToken cancellationToken = default)
    {
        var resourceName = $"{toolchainName}.json";
        var json = await _embeddedResources.ReadResourceAsStringAsync(resourceName, cancellationToken);
        if (json is null)
            return null;

        return JsonSerializer.Deserialize(json, ToolchainConfigJsonContext.Default.ToolchainConfig);
    }

    public bool EmbeddedExists(string toolchainName)
    {
        return _embeddedResources.ResourceExists($"{toolchainName}.json");
    }

    public IEnumerable<string> ListEmbeddedToolchains()
    {
        return _embeddedResources
            .ListResources()
            .Where(r => r.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .Select(r => Path.GetFileNameWithoutExtension(r));
    }

    private static string ResolveConfigPath(string toolchainName, string? workingDirectory)
    {
        var fileName = toolchainName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? toolchainName
            : $"{toolchainName}.json";

        return Path.IsPathRooted(fileName)
            ? fileName
            : Path.Combine(workingDirectory ?? Directory.GetCurrentDirectory(), fileName);
    }
}