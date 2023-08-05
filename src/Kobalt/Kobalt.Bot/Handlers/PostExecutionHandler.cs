using Kobalt.Shared.Results;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Services;
using Remora.Results;
using IResult = Remora.Results.IResult;

namespace Kobalt.Bot.Handlers;

public class PostExecutionHandler : IPostExecutionEvent
{
    private readonly IDiscordRestInteractionAPI _interactions;
    private readonly ILogger<PostExecutionHandler> _logger;

    public PostExecutionHandler(IDiscordRestInteractionAPI interactions, ILogger<PostExecutionHandler> logger)
    {
        _interactions = interactions;
        _logger = logger;
    }

    public async Task<Result> AfterExecutionAsync(ICommandContext context, IResult commandResult, CancellationToken ct)
    {
        if (!commandResult.IsSuccess)
        {
            //TODO: Proper error handling, potentially returning a message to the user?
            _logger.LogError("Command {Command} failed with error {Error}", context.Command.Command.Node.CommandMethod.Name, commandResult.Error);
        }

        if (commandResult is Result<FeedbackResult> feedbackMessage && context is IInteractionContext interactionContext)
        {
            return (Result)await _interactions.CreateFollowupMessageAsync
            (
                interactionContext.Interaction.ApplicationID,
                interactionContext.Interaction.Token,
                feedbackMessage.Entity.Message,
                embeds: feedbackMessage.Entity.Embeds.Map(s => (IReadOnlyList<IEmbed>)s.ToArray()),
                ct: ct
            );
        }

        return Result.FromSuccess();
    }
}
