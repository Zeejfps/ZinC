using System.CommandLine;
using System.CommandLine.Help;

var rootCommand = new RootCommand("The simplest 'C' build tool around.")
{
    Action = new HelpAction(),
    Subcommands =
    {
        new Command("init", "Initializes a new project."),
        new Command("build", "Builds the project."),
        new Command("run","Builds and runs the project."),
    }
};

var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();