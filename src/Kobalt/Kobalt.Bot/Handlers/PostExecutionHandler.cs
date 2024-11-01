using Kobalt.Shared.Results;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Services;
using Remora.Results;
using IResult = Remora.Results.IResult;

namespace Kobalt.Bot.Handlers;

public class PostExecutionHandler : IPostExecutionEvent, IPreparationErrorEvent
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

    public async Task<Result> PreparationFailed
    (
        IOperationContext context,
        IResult preparationResult,
        CancellationToken ct = default
    )
    {
        _logger.LogError("Preparation failed with error {Error}", preparationResult.Error);
        if (preparationResult.Error!.IsUserOrEnvironmentError())
        {
            // If the error is a user or environment error, we should send a message to the user
            if (context is IInteractionContext ic)
            {
                return await SendErrorMessageAsync(ic, preparationResult.Inner!.Inner!.Error!.Message, ct);
            }
        }

        return Result.FromSuccess();
    }

    private async Task<Result> SendErrorMessageAsync(IInteractionContext context, string message, CancellationToken ct)
    {
        if (!context.HasRespondedToInteraction)
        {
            return await _interactions.CreateInteractionResponseAsync
            (
                context.Interaction.ID,
                context.Interaction.Token,
                new InteractionResponse
                (
                    InteractionCallbackType.ChannelMessageWithSource,
                    new(new InteractionMessageCallbackData(Content: message, Flags: MessageFlags.Ephemeral))
                )
            );
        }

        return (Result)await _interactions.CreateFollowupMessageAsync
        (
            context.Interaction.ApplicationID,
            context.Interaction.Token,
            message,
            ct: ct
        );
    }
}
