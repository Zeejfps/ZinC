using System.Reflection;

namespace ZinC.Cli.Resources;

internal sealed class EmbeddedResourcesService
{
    private const string ResourcePrefix = "ZinC.Cli.Resources.Embedded.";
    private readonly Assembly _assembly;

    public EmbeddedResourcesService()
    {
        _assembly = Assembly.GetExecutingAssembly();
    }

    public string? ReadResourceAsString(string name)
    {
        var resourceName = ResourcePrefix + name;
        using var stream = _assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            return null;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
    
    public async Task<string?> ReadResourceAsStringAsync(string name, CancellationToken cancellationToken = default)
    {
        var resourceName = ResourcePrefix + name;
        await using var stream = _assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            return null;

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    public IEnumerable<string> ListResources()
    {
        return _assembly
            .GetManifestResourceNames()
            .Where(n => n.StartsWith(ResourcePrefix))
            .Select(n => n[ResourcePrefix.Length..]);
    }

    public bool ResourceExists(string name)
    {
        var resourceName = ResourcePrefix + name;
        return _assembly.GetManifestResourceNames().Contains(resourceName);
    }
}