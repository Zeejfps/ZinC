using System.CommandLine;
using ZinC.Cli.Console;
using ZinC.Cli.Logging;

namespace ZinC.Cli.Build;

internal sealed class CompileCommandsAction : ZincCommandAction
{
    public required Argument<string> ToolchainArgument { get; init; }
    public required Argument<string> PlatformArgument { get; init; }
    public required Argument<string> ModeArgument { get; init; }

    public override Argument[] Arguments => [ToolchainArgument, PlatformArgument, ModeArgument];
    public override Option[] Options => [];

    private readonly IConsole _console;

    public CompileCommandsAction(IConsole console, ILogger logger) : base(console, logger)
    {
        _console = console;
    }

    protected override async Task<int> OnInvokedAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var toolchainName = parseResult.GetRequiredValue(ToolchainArgument);
        var platform = parseResult.GetRequiredValue(PlatformArgument);
        var mode = parseResult.GetRequiredValue(ModeArgument);

        // Create build context
        var factory = new BuildContextFactory();
        var contextResult = await factory.CreateContextAsync(toolchainName, platform, mode, cancellationToken);

        if (!contextResult.IsSuccess)
        {
            WriteErrorLine(contextResult.ErrorMessage!);
            return 1;
        }

        var context = contextResult.Context!;

        // Generate compile_commands.json
        WriteLine("Generating compile_commands.json...");

        var buildService = new BuildService(_console);
        var success = await buildService.GenerateCompileCommandsAsync(context, cancellationToken: cancellationToken);

        if (success)
        {
            WriteLine("Successfully generated compile_commands.json");
            return 0;
        }

        WriteErrorLine("Failed to generate compile_commands.json");
        return 1;
    }
}
