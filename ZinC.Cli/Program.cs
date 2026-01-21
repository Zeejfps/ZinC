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

var initAction = new SetupAction(console, logger)
{
    ProjectTypeArgument = new Argument<ProjectType>("project-type")
};
var buildAction = new BuildAction(console, logger);
var runAction = new RunAction(console, logger);

var rootCommand = new RootCommand("The simplest 'C' build tool around.")
{
    Action = new HelpAction(),
    Subcommands =
    {
        new Command("init", "Initializes a new project.")
            .WithAction(initAction),
        
        new Command("build", "Builds the project.")
            .WithAction(buildAction),
        
        new Command("run","Builds and runs the project.")
            .WithAction(runAction),
    }
};

var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();