using System.CommandLine;
using System.CommandLine.Help;
using ZinC.Cli.Build;
using ZinC.Cli.Console;
using ZinC.Cli.Extensions;
using ZinC.Cli.Logging;
using ZinC.Cli.Run;
using ZinC.Cli.Setup;

var console = new DefaultConsole();
var logger = new ConsoleLogger();

var projectTypeArg = new ProjectTypeArgument("project-type");
var initAction = new SetupAction(console, logger)
{
    ProjectTypeArgument = projectTypeArg,
    ArtifactNameOption = new Option<string>("--name", "-n")
    {
        Required = true,
        Description = "The name of the artifact."
    },
    CompilerOption = new Option<string>("--compiler", "-c")
    {
        Required = true,
        Description = "The compiler to use.",
    }
};
var buildAction = new BuildAction(console, logger)
{
    ModeOption = new Option<string>("--mode", "-m")
    {
        Description = "The build mode (release, debug, etc...)",
        Required = true
    },
    PlatformOption = new Option<string>("--platform", "-p")
    {
        Description = "The platform to build for (windows, osx, linux, wasm, etc...)",
        Required = true
    },
    CompilerOption = new Option<string>("--config", "-c")
    {
        Description = "The compiler to use.",
        Required = true
    }
};

var runAction = new RunAction(console, logger);

var rootCommand = new RootCommand("The simplest 'C' build tool around.")
{
    Action = new HelpAction(),
    Subcommands =
    {
        new Command("setup", "Sets up a new project.")
            .WithAction(initAction),
        
        new Command("build", "Builds the project.")
            .WithAction(buildAction),
        
        new Command("run","Builds and runs the project.")
            .WithAction(runAction),
    }
};

var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();