using System.CommandLine;
using System.CommandLine.Invocation;

namespace ZinC.Cli.Middleware;

internal abstract class BaseMiddleware : IMiddleware
{
    private readonly AsynchronousCommandLineAction _nextAction;

    protected BaseMiddleware(AsynchronousCommandLineAction nextAction)
    {
        _nextAction = nextAction;
    }

    public Command Wrap(Command command)
    {
        var nextCommand = command.Action;
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var result = await _nextAction.InvokeAsync(parseResult, cancellationToken);
            
            if (nextCommand is AsynchronousCommandLineAction asynchronousCommandLineAction)
                return await asynchronousCommandLineAction.InvokeAsync(parseResult, cancellationToken);
            
            if (nextCommand is SynchronousCommandLineAction synchronousCommandLineAction)
                return synchronousCommandLineAction.Invoke(parseResult);
            
            return result;
        });
        return command;
    }
}