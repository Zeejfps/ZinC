using System.CommandLine;
using ZinC.Cli.Config;
using ZinC.Cli.Console;
using ZinC.Cli.Logging;

namespace ZinC.Cli.Configure;

internal sealed class ConfigureAction : ZincCommandAction
{
    public required Argument<string> ToolchainArgument { get; init; }

    public override Argument[] Arguments => [ToolchainArgument];

    public ConfigureAction(IConsole console, ILogger logger) : base(console, logger)
    {
    }

    protected override async Task<int> OnInvokedAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var toolchainName = parseResult.GetRequiredValue(ToolchainArgument);

        var toolchainService = new ToolchainConfigService();

        // Check if local file already exists
        var localPath = Path.Combine(Directory.GetCurrentDirectory(), $"{toolchainName}.json");
        if (File.Exists(localPath))
        {
            WriteErrorLine($"Toolchain config already exists: {localPath}");
            return 1;
        }

        // Load from embedded resources
        var config = await toolchainService.LoadFromEmbeddedAsync(toolchainName, cancellationToken);
        if (config is null)
        {
            var available = string.Join(", ", toolchainService.ListEmbeddedToolchains());
            WriteErrorLine($"Unknown toolchain: {toolchainName}. Available: {available}");
            return 1;
        }

        // Save to local file
        await toolchainService.SaveAsync(config, toolchainName, cancellationToken: cancellationToken);
        WriteLine($"Created toolchain config: {localPath}");

        return 0;
    }
}
