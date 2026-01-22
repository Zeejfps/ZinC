using System.Text.Json;

namespace ZinC.Cli.Config;

internal sealed class ProjectConfigService
{
    private const string DefaultConfigFileName = "zinc.json";

    public ProjectConfig? Load(string? configPath = null, string? workingDirectory = null)
    {
        var path = ResolveConfigPath(configPath, workingDirectory);

        if (!File.Exists(path))
        {
            return null;
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize(json, ProjectConfigJsonContext.Default.ProjectConfig);
    }

    public async Task<ProjectConfig?> LoadAsync(string? configPath = null, string? workingDirectory = null, CancellationToken cancellationToken = default)
    {
        var path = ResolveConfigPath(configPath, workingDirectory);

        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync(stream, ProjectConfigJsonContext.Default.ProjectConfig, cancellationToken);
    }

    public void Save(ProjectConfig config, string? configPath = null, string? workingDirectory = null)
    {
        var path = ResolveConfigPath(configPath, workingDirectory);
        var json = JsonSerializer.Serialize(config, ProjectConfigJsonContext.Default.ProjectConfig);
        File.WriteAllText(path, json);
    }

    public async Task SaveAsync(ProjectConfig config, string? configPath = null, string? workingDirectory = null, CancellationToken cancellationToken = default)
    {
        var path = ResolveConfigPath(configPath, workingDirectory);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, config, ProjectConfigJsonContext.Default.ProjectConfig, cancellationToken);
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
