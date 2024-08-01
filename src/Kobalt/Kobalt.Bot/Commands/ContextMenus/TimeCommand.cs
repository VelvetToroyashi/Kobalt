using System.Text;
using Kobalt.Shared.Extensions;
using Kobalt.Shared.MediatR.Users;
using Kobalt.Shared.Results;
using Kobalt.Shared.Services;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace Kobalt.Bot.Commands.ContextMenus;

public class TimeCommand : CommandGroup
{
    private const string TimezoneNotFoundOrSharedError = "The author has not set a timezone, or does not wish to share it.\n " +
                                                         "Ask them to run `/user-settings timezone set` with `share_timezone` to true.";

    private const string TimezoneNotSetForCurrentUserError = "You have not set a timezone. Please run `/user-settings timezone set` to set your timezone.";

    private readonly IMediator _mediator;
    private readonly IInteractionContext _context;

    public TimeCommand(IMediator mediator, IInteractionContext context)
    {
        _mediator = mediator;
        _context = context;
    }

    [Command("Localize Time")]
    [CommandType(ApplicationCommandType.Message)]
    public async Task<Result<FeedbackResult>> LocalizeTimeAsync(IMessage message)
    {
        var contextUser = _context.Interaction.Member.Map(m => m.User).OrDefault(_context.Interaction.User).Value;

        var authorResult = await _mediator.Send(new GetUserRequest(message.Author.ID));
        var currentUserResult = await _mediator.Send(new GetUserRequest(contextUser.ID));

        if (!authorResult.IsDefined(out var user) || user.Timezone is not {} timezone || !user.DisplayTimezone)
        {
            return new FeedbackResult(TimezoneNotFoundOrSharedError);
        }

        if (!currentUserResult.IsDefined(out var currentUser) || currentUser.Timezone is not {} currentUserTimezone)
        {
            return new FeedbackResult(TimezoneNotSetForCurrentUserError);
        }

        var localizedTimesResult = TimeHelper.ExtractTimes(message.Content, timezone);

        if (!localizedTimesResult.IsDefined(out var localTimes) || !localTimes.Any())
        {
            return new FeedbackResult("I don't see any times in that message.");
        }

        var timeMessage = new StringBuilder();

        timeMessage.AppendLine("Here are the times I found in the message. Hope it helps.")
                   .AppendLine();

        var localTimezone = currentUserTimezone.ToTimeSpan();

        foreach (var time in localTimes)
        {
            var text = message.Content[time.Position];
            var timeString = time.Time.Match
            (
                dto => (dto + localTimezone).ToTimestamp(TimestampFormat.ShortTime),
                dtoRange => $"{(dtoRange.Start + localTimezone).ToTimestamp(TimestampFormat.ShortDateTime)} - " +
                            $"{(dtoRange.End + localTimezone).ToTimestamp(TimestampFormat.ShortDateTime)}"
            );

            timeMessage.AppendLine($"`{text}` ➜ {timeString}");
        }

        return new FeedbackResult(timeMessage.ToString());
    }
}
