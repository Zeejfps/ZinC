using System.CommandLine;
using ZinC.Cli.Middleware;

namespace ZinC.Cli.Extensions;

internal static class CommandExtensions
{
    extension(Command command)
    {
        public Command WithAction(ZincCommandAction action)
        {
            command.Action = action;
            foreach (var option in action.Options)
            {
                command.Options.Add(option);
            }
            foreach (var argument in action.Arguments)
            {
                command.Arguments.Add(argument);
            }
            return command;
        }

        public Command Use(IMiddleware middleware)
        {
            return middleware.Wrap(command);
        }
    }
}