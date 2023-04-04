using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Services;
using IResult = Remora.Results.IResult;

namespace Kobalt.Bot.Handlers;

public class PostExecutionHandler : IPostExecutionEvent
{
    private readonly ILogger<PostExecutionHandler> _logger;
    
    public PostExecutionHandler(ILogger<PostExecutionHandler> logger)
    {
        _logger = logger;
    }

    public Task<Result> AfterExecutionAsync(ICommandContext context, IResult commandResult, CancellationToken ct)
    {

        if (!commandResult.IsSuccess)
        {
            //TODO: Proper error handling, potentially returning a message to the user?
            _logger.LogError("Command {Command} failed with error {Error}", context.Command.Command.Node.CommandMethod.Name, commandResult.Error);
        }

        return Task.FromResult(Result.FromSuccess());
    }
}
