using System.ComponentModel;
using Kobalt.Plugins.Infractions.Services;
using Kobalt.Shared.Conditions;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace Kobalt.Plugins.Infractions.Commands;

public class KickCommand : CommandGroup
{
    private readonly IInteractionContext _context;
    private readonly InfractionAPIService _apiService;
    private readonly IDiscordRestInteractionAPI _interactions;

    public KickCommand(IInteractionContext context, InfractionAPIService apiService, IDiscordRestInteractionAPI interactions)
    {
        _context = context;
        _apiService = apiService;
        _interactions = interactions;
    }

    [Command("kick")]
    [Description("Kicks a user from the guild.")]
    [RequireContext(ChannelContext.Guild)]
    public async Task<Result> KickAsync
    (
        [Description("The user to be kicked.")]
        [EnsureHierarchy(HierarchyTarget.Self, HierarchyLevel.Higher)]
        [EnsureHierarchy(HierarchyTarget.Invoker, HierarchyLevel.Higher)]
        IUser target,

        [Description("The reason for the kick.")]
        string reason = "Not Given."
    )
    {
        var result = await _apiService.KickUserAsync(_context.Interaction.GuildID.Value, target, _context.Interaction.Member.Value.User.Value, reason);

        if (!result.IsSuccess)
        {
            return result;
        }

        return (Result)await _interactions.EditOriginalInteractionResponseAsync
        (
            _context.Interaction.ApplicationID,
            _context.Interaction.Token,
            "User kicked successfully."
        );
    }
}
