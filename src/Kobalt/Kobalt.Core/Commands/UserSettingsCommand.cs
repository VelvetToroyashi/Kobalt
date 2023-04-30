using System.ComponentModel;
using Kobalt.Core.Services;
using Kobalt.Data.Mediator;
using MediatR;
using NodaTime;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Rest.Core;

namespace Kobalt.Core.Commands;

[Group("user-settings")]
[Description("Settings about you.")]
public class UserSettingsCommand : CommandGroup
{
    [Group("timezone")]
    public class Timezone : CommandGroup
    {
        private readonly IMediator _mediator;
        private readonly IInteractionContext _context;
        private readonly IDateTimeZoneProvider _timezoneProvider;
        private readonly IDiscordRestInteractionAPI _interactions;
        
        public Timezone(IMediator mediator, IInteractionContext context, IDateTimeZoneProvider timezoneProvider, IDiscordRestInteractionAPI interactions)
        {
            _mediator = mediator;
            _context = context;
            _timezoneProvider = timezoneProvider;
            _interactions = interactions;
        }

        [Command("set")]
        [Description("Sets your timezone. Either UTC offset or a timezone name.")]
        public async Task<Result> SetTimezoneAsync
        (
            [Option("timezone")]
            [Description("Your timezone.")]
            [AutocompleteProvider("TimezoneAutocomplete")]
            string timezone,
            
            [Option("share_timezone")]
            [Description("Whether you want to share your timezone with other users.")]
            bool? displayTimezone = null
        )
        {
            if (!_context.TryGetUserID(out var userId))
            {
                return new InvalidOperationException("Could not get user ID.");
            }

            var timezoneResult = TimeHelper.GetDateTimeZoneFromString(timezone, _timezoneProvider);
            
            if (!timezoneResult.IsDefined(out var extractedTimezone))
            {
                return (Result)timezoneResult;
            }
            
            await _mediator.Send(new UpdateUser.Request(userId.Value, timezone, displayTimezone.AsOptional()));

            var now = Instant.FromDateTimeOffset(DateTimeOffset.UtcNow);
            var content = $"Done. It should be {DateTimeOffset.UtcNow + extractedTimezone.GetUtcOffset(now).ToTimeSpan():f} for you now.";

            return (Result)await _interactions.EditOriginalInteractionResponseAsync
            (
                _context.Interaction.ApplicationID,
                _context.Interaction.Token,
                content: content
            );
        }
    }
}
