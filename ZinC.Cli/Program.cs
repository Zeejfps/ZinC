using System.CommandLine;
using System.CommandLine.Help;
using ZinC.Cli.Build;
using ZinC.Cli.Console;
using ZinC.Cli.Extensions;
using ZinC.Cli.Logging;
using ZinC.Cli.Setup;
using ZinC.Cli.Toolchains;

var console = new DefaultConsole();
var logger = new ConsoleLogger();

var projectTypeArg = new ProjectTypeArgument("project-type");
var setupAction = new SetupAction(console, logger)
{
    ProjectTypeArgument = projectTypeArg,
    ArtifactNameOption = new Option<string>("--name", "-n")
    {
        Description = "The name of the artifact."
    }
};

var toolchainArg = new Argument<string>("toolchain")
{
    Description = "The toolchain to use (gcc, clang, msvc, etc.)"
};

var platformArg = new Argument<string>("platform")
{
    Description = "The platform to build for (windows, linux, macos, wasm, etc.)"
};

var modeArg = new Argument<string>("mode")
{
    Description = "The build mode (debug, release, etc.)"
};

var runOption = new Option<bool>("--run", "-r")
{
    Description = "Run the artifact after building"
};

var compileCommandsOption = new Option<bool>("--compile-commands", "-cc")
{
    Description = "Generate compile_commands.json for IDE intellisense"
};

var verboseOption = new Option<bool>("--verbose", "-v")
{
    Description = "Show full compile and link commands"
};

var buildAction = new BuildAction(console, logger)
{
    ToolchainArgument = toolchainArg,
    PlatformArgument = platformArg,
    ModeArgument = modeArg,
    RunOption = runOption,
    CompileCommandsOption = compileCommandsOption,
    VerboseOption = verboseOption,
};

var configureAction = new ConfigureAction(console, logger)
{
    ToolchainArgument = new Argument<string>("toolchain")
    {
        Description = "The toolchain to eject (gcc, msvc, etc.)"
    }
};

var toolchainsAction = new ToolchainsAction(console, logger)
{
    ToolchainArgument = new Argument<string?>("toolchain")
    {
        Description = "The toolchain to show details for",
        Arity = ArgumentArity.ZeroOrOne
    }
};

var rootCommand = new RootCommand("The simplest 'C' build tool around.")
{
    Action = new HelpAction(),
    Subcommands =
    {
        new Command("setup", "Sets up a new project.")
            .WithAction(setupAction),

        new Command("build", "Builds the project.")
            .WithAction(buildAction),

        new Command("configure", "Ejects a toolchain config for customization.")
            .WithAction(configureAction),

        new Command("toolchains", "Lists available toolchains and their configurations.")
            .WithAction(toolchainsAction),
    }
};

var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();