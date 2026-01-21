using System.CommandLine;

namespace ZinC.Cli;

internal interface IMiddleware
{
    Command Wrap(Command command);
}