using Kobalt.Infractions.Shared;
using Kobalt.Plugins.Core.Data.Mediator;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;

namespace Kobalt.Plugins.Core.Commands;

public enum GuildConfigPhishingActionType
{
    Kick = InfractionType.Kick,
    Ban = InfractionType.Ban,
    Mute = InfractionType.Mute
}

[Group("guild-settings")]
[RequireContext(ChannelContext.Guild)]
public class GuildSettingsCommands : CommandGroup
{
    private readonly IMediator _mediator;
    private readonly IInteractionContext _context;
    private readonly IDiscordRestInteractionAPI _interactions;

    public GuildSettingsCommands(IMediator mediator, IInteractionContext context, IDiscordRestInteractionAPI interactions)
    {
        _mediator = mediator;
        _context = context;
        _interactions = interactions;
    }

    [Command("phishing")]
    public async Task<Result> UpdatePhishingConfigAsync
    (
        [Option("scan_users")]
        bool? ScanUsers = default,

        [Option("scan_links")]
        bool? ScanLinks = default,

        [Option("action")]
        GuildConfigPhishingActionType? Action = default
    )
    {
        var users = ScanUsers.AsOptional();
        var links = ScanLinks.AsOptional();
        var action = Action.AsOptional().Map(m => (InfractionType)m);

        var result = await _mediator.Send(new UpdateGuild.AntiPhishing.Request(_context.Interaction.GuildID.Value, users, links, action));

        if (!result.IsSuccess)
        {
            return result;
        }

        return (Result)await _interactions.CreateFollowupMessageAsync
        (
            _context.Interaction.ApplicationID,
            _context.Interaction.Token,
            "<:_:866599434729029642> Consider it done.",
            flags: MessageFlags.Ephemeral
        );
    }
}
