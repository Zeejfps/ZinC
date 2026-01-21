using System.CommandLine;

namespace ZinC.Cli.Middleware;

internal interface IMiddleware
{
    Command Wrap(Command command);
}