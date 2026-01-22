using System.CommandLine;
using System.CommandLine.Help;
using ZinC.Cli.Build;
using ZinC.Cli.Configure;
using ZinC.Cli.Console;
using ZinC.Cli.Extensions;
using ZinC.Cli.Logging;
using ZinC.Cli.Run;
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

var modeOption = new Option<string>("--mode", "-m")
{
    Description = "The build mode (release, debug, etc...)",
    Required = true
};

var platformOption = new Option<string>("--platform", "-p")
{
    Description = "The platform to build for (windows, osx, linux, wasm, etc...)",
    Required = true
};

var toolchainOption = new Option<string>("--toolchain", "-t")
{
    Description = "The toolchain to use (gcc, msvc, etc.)",
    Required = true
};

var buildAction = new BuildAction(console, logger)
{
    ModeOption = modeOption,
    PlatformOption = platformOption,
    ToolchainOption = toolchainOption,
};

var runAction = new RunAction(console, logger)
{
    ModeOption = modeOption,
    PlatformOption = platformOption,
    ToolchainOption = toolchainOption,
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
        
        new Command("run","Builds and runs the project.")
            .WithAction(runAction),

        new Command("configure", "Ejects a toolchain config for customization.")
            .WithAction(configureAction),

        new Command("toolchains", "Lists available toolchains and their configurations.")
            .WithAction(toolchainsAction),
    }
};

var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();