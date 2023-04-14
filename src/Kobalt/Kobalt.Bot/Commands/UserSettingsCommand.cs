using System.ComponentModel;
using Kobalt.Data.Mediator;
using Mediator;
using NodaTime;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Rest.Core;

namespace Kobalt.Bot.Commands;

[Group("user-settings")]
[Description("Settings about you.")]
public class UserSettingsCommand : CommandGroup
{
    [Group("timezone")]
    public class Timezone : CommandGroup
    {
        private readonly IMediator _mediator;
        private readonly IInteractionContext _context;
        private readonly IDiscordRestInteractionAPI _interactions;
        
        public Timezone(IMediator mediator, IInteractionContext context, IDiscordRestInteractionAPI interactions)
        {
            _mediator = mediator;
            _context = context;
            _interactions = interactions;
        }

        [Command("set")]
        [Description("Sets your timezone. Either UTC offset or a timezone name.")]
        public async Task<Result> SetTimezoneAsync
        (
            [Option("timezone")]
            [Description("Your timezone.")]
            [DiscordTypeHint(TypeHint.String)]
            [AutocompleteProvider("TimezoneAutocomplete")]
            Offset timezone,
            
            [Option("share_timezone")]
            [Description("Whether you want to share your timezone with other users.")]
            bool? displayTimezone = null
        )
        {
            if (!_context.TryGetUserID(out var userId))
            {
                return new InvalidOperationException("Could not get user ID.");
            }
            
            await _mediator.Send(new UpdateUser.Request(userId.Value, timezone, displayTimezone.AsOptional()));

            var content = $"Done. It should be {DateTimeOffset.UtcNow + timezone.ToTimeSpan():f} for you now.";

            return (Result)await _interactions.EditOriginalInteractionResponseAsync
            (
                _context.Interaction.ApplicationID,
                _context.Interaction.Token,
                content: content
            );
        }
    }

}
