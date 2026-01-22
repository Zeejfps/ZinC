using System.CommandLine;
using ZinC.Cli.Config;
using ZinC.Cli.Console;
using ZinC.Cli.Logging;

namespace ZinC.Cli.Toolchains;

internal sealed class ToolchainsAction : ZincCommandAction
{
    public required Argument<string?> ToolchainArgument { get; init; }

    public override Argument[] Arguments => [ToolchainArgument];

    public ToolchainsAction(IConsole console, ILogger logger) : base(console, logger)
    {
    }

    protected override async Task<int> OnInvokedAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var toolchainName = parseResult.GetValue(ToolchainArgument);
        var toolchainService = new ToolchainConfigService();

        if (string.IsNullOrEmpty(toolchainName))
        {
            return await ListToolchainsAsync(toolchainService, cancellationToken);
        }

        return await ShowToolchainDetailsAsync(toolchainService, toolchainName, cancellationToken);
    }

    private async Task<int> ListToolchainsAsync(ToolchainConfigService service, CancellationToken cancellationToken)
    {
        WriteLine("Available toolchains:");
        WriteLine("");

        foreach (var name in service.ListEmbeddedToolchains())
        {
            var config = await service.LoadFromEmbeddedAsync(name, cancellationToken);
            if (config is null) continue;

            var modes = config.Modes?.Keys ?? Enumerable.Empty<string>();
            var platforms = config.Platforms?.Keys ?? Enumerable.Empty<string>();

            var modesStr = string.Join(", ", modes);
            var platformsStr = string.Join(", ", platforms);

            WriteLine($"  {name}");
            WriteLine($"    Compiler:  {config.CompilerExe}");
            WriteLine($"    Modes:     {modesStr}");
            WriteLine($"    Platforms: {platformsStr}");
            WriteLine("");
        }

        WriteLine("Use 'zinc toolchains <name>' for detailed information.");
        WriteLine("Use 'zinc configure <name>' to eject a toolchain for customization.");

        return 0;
    }

    private async Task<int> ShowToolchainDetailsAsync(
        ToolchainConfigService service,
        string toolchainName,
        CancellationToken cancellationToken)
    {
        var config = await service.LoadAsync(toolchainName, cancellationToken: cancellationToken);
        if (config is null)
        {
            var available = string.Join(", ", service.ListEmbeddedToolchains());
            WriteErrorLine($"Unknown toolchain: {toolchainName}");
            WriteErrorLine($"Available: {available}");
            return 1;
        }

        WriteLine($"Toolchain: {toolchainName}");
        WriteLine($"Compiler:  {config.CompilerExe}");
        WriteLine("");

        // Base flags
        if (config.Flags is { Count: > 0 })
        {
            WriteLine("Base flags:");
            WriteLine($"  {string.Join(" ", config.Flags)}");
            WriteLine("");
        }

        // Base link flags
        if (config.LinkFlags is { Count: > 0 })
        {
            WriteLine("Base link flags:");
            WriteLine($"  {string.Join(" ", config.LinkFlags)}");
            WriteLine("");
        }

        // Base defines
        if (config.Defines is { Count: > 0 })
        {
            WriteLine("Base defines:");
            WriteLine($"  {string.Join(" ", config.Defines)}");
            WriteLine("");
        }

        // Modes
        if (config.Modes is { Count: > 0 })
        {
            WriteLine("Modes:");
            foreach (var (modeName, mode) in config.Modes)
            {
                WriteLine($"  {modeName}:");
                if (mode.Flags is { Count: > 0 })
                    WriteLine($"    Flags:      {string.Join(" ", mode.Flags)}");
                if (mode.Defines is { Count: > 0 })
                    WriteLine($"    Defines:    {string.Join(" ", mode.Defines)}");
                if (mode.LinkFlags is { Count: > 0 })
                    WriteLine($"    Link flags: {string.Join(" ", mode.LinkFlags)}");
            }
            WriteLine("");
        }

        // Platforms
        if (config.Platforms is { Count: > 0 })
        {
            WriteLine("Platforms:");
            foreach (var (platformName, platform) in config.Platforms)
            {
                WriteLine($"  {platformName}:");
                WriteLine($"    Extensions: exe={platform.ArtifactExtension ?? "(none)"}, " +
                          $"static={platform.StaticLibExtension}, " +
                          $"shared={platform.SharedLibExtension}, " +
                          $"obj={platform.ObjectExtension}");
                if (platform.Flags is { Count: > 0 })
                    WriteLine($"    Flags:      {string.Join(" ", platform.Flags)}");
                if (platform.Defines is { Count: > 0 })
                    WriteLine($"    Defines:    {string.Join(" ", platform.Defines)}");
                if (platform.Libs is { Count: > 0 })
                    WriteLine($"    Libs:       {string.Join(" ", platform.Libs)}");
                if (platform.LinkFlags is { Count: > 0 })
                    WriteLine($"    Link flags: {string.Join(" ", platform.LinkFlags)}");
            }
            WriteLine("");
        }

        // Artifact types
        if (config.ArtifactTypes is { Count: > 0 })
        {
            WriteLine("Artifact types:");
            foreach (var (typeName, artifactType) in config.ArtifactTypes)
            {
                WriteLine($"  {typeName}:");
                if (artifactType.UseArchiver)
                {
                    WriteLine($"    Uses archiver (ar)");
                    if (artifactType.ArchiverFlags is { Count: > 0 })
                        WriteLine($"    Archiver flags: {string.Join(" ", artifactType.ArchiverFlags)}");
                }
                else
                {
                    if (artifactType.Flags is { Count: > 0 })
                        WriteLine($"    Flags:      {string.Join(" ", artifactType.Flags)}");
                    if (artifactType.LinkFlags is { Count: > 0 })
                        WriteLine($"    Link flags: {string.Join(" ", artifactType.LinkFlags)}");
                }
            }
        }

        return 0;
    }
}
