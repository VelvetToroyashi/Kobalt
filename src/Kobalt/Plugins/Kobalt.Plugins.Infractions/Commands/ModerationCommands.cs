using System.ComponentModel;
using Kobalt.Plugins.Infractions.Services;
using Kobalt.Shared.Conditions;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace Kobalt.Plugins.Infractions.Commands;

[Group("moderation")]
[RequireContext(ChannelContext.Guild)]
[RequireDiscordPermission(DiscordPermission.ManageRoles, DiscordPermission.KickMembers)]
[RequireBotDiscordPermissions(DiscordPermission.BanMembers)]
public class ModerationCommands : CommandGroup
{
    private readonly IInteractionContext _context;
    private readonly InfractionAPIService _apiService;
    private readonly IDiscordRestInteractionAPI _interactions;

    public ModerationCommands(IInteractionContext context, InfractionAPIService apiService, IDiscordRestInteractionAPI interactions)
    {
        _context = context;
        _apiService = apiService;
        _interactions = interactions;
    }

    [Command("kick")]
    [Description("Kicks a user from the guild.")]
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
        var result = await _apiService.AddUserKickAsync(_context.Interaction.GuildID.Value, target, _context.Interaction.Member.Value.User.Value, reason);

        if (!result.IsSuccess)
        {
            return result;
        }

        await _apiService.TryEscalateInfractionAsync(_context.Interaction.GuildID.Value, target);

        return (Result)await _interactions.EditOriginalInteractionResponseAsync
        (
            _context.Interaction.ApplicationID,
            _context.Interaction.Token,
            "User kicked successfully."
        );
    }

    [Command("ban")]
    [Description("Bans a user from the guild.")]
    public async Task<Result> BanAsync
    (
        [Description("The user to be banned.")]
        [EnsureHierarchy(HierarchyTarget.Self, HierarchyLevel.Higher)]
        [EnsureHierarchy(HierarchyTarget.Invoker, HierarchyLevel.Higher)]
        IUser target,

        [Description("The reason for the ban.")]
        string reason = "Not Given.",


        [Description("The duration of the ban.")]
        [DiscordTypeHint(TypeHint.String)]
        TimeSpan? duration = null,

        [Description("How much of the user's history to delete.")]
        [DiscordTypeHint(TypeHint.String)]
        TimeSpan? delete = null
    )
    {
        var result = await _apiService.AddUserBanAsync(_context.Interaction.GuildID.Value, target, _context.Interaction.Member.Value.User.Value, reason, duration, delete);

        if (!result.IsSuccess)
        {
            return result;
        }

        await _apiService.TryEscalateInfractionAsync(_context.Interaction.GuildID.Value, target);

        return (Result)await _interactions.EditOriginalInteractionResponseAsync
        (
            _context.Interaction.ApplicationID,
            _context.Interaction.Token,
            "User banned successfully."
        );
    }

    [Command("mute")]
    [Description("Mutes a user on the guild.")]
    public async Task<Result> MuteAsync
    (
        [Description("The user to be muted.")]
        [EnsureHierarchy(HierarchyTarget.Self, HierarchyLevel.Higher)]
        [EnsureHierarchy(HierarchyTarget.Invoker, HierarchyLevel.Higher)]
        IUser target,

        [Description("How long to mute. Up to 4 weeks.")]
        [DiscordTypeHint(TypeHint.String)]
        TimeSpan duration,

        [Description("The reason for the mute.")]
        string reason = "Not Given."
    )
    {
        var result = await _apiService.AddUserMuteAsync(_context.Interaction.GuildID.Value, target, _context.Interaction.Member.Value.User.Value, reason, duration);

        if (!result.IsSuccess)
        {
            return result;
        }

        await _apiService.TryEscalateInfractionAsync(_context.Interaction.GuildID.Value, target);

        return (Result)await _interactions.EditOriginalInteractionResponseAsync
        (
            _context.Interaction.ApplicationID,
            _context.Interaction.Token,
            "User muted successfully."
        );
    }

    [Command("warn")]
    [Description("Warns a user on the guild.")]
    public async Task<Result> WarnAsync
    (
        [Description("The user to be warned.")]
        [EnsureHierarchy(HierarchyTarget.Self, HierarchyLevel.Higher)]
        [EnsureHierarchy(HierarchyTarget.Invoker, HierarchyLevel.Higher)]
        IUser target,

        [Description("The reason for the warning.")]
        string reason = "Not Given."
    )
    {
        var result = await _apiService.AddUserStrikeAsync(_context.Interaction.GuildID.Value, target, _context.Interaction.Member.Value.User.Value, reason);

        if (!result.IsSuccess)
        {
            return result;
        }

        await _apiService.TryEscalateInfractionAsync(_context.Interaction.GuildID.Value, target);

        return (Result)await _interactions.EditOriginalInteractionResponseAsync
        (
            _context.Interaction.ApplicationID,
            _context.Interaction.Token,
            "User warned successfully."
        );
    }

    [Command("note")]
    [Description("Warns a user on the guild.")]
    public async Task<Result> NoteAsync
    (
        [Description("The user to be warned.")]
        [EnsureHierarchy(HierarchyTarget.Self, HierarchyLevel.Higher)]
        [EnsureHierarchy(HierarchyTarget.Invoker, HierarchyLevel.Higher)]
        IUser target,

        [Description("The reason for the warning.")]
        string reason = "Not Given.",

        [Option("case_id")]
        [Description("The ID of the case to attach this note to.")]
        int? caseID = null
    )
    {
        var result = await _apiService.AddUserStrikeAsync(_context.Interaction.GuildID.Value, target, _context.Interaction.Member.Value.User.Value, reason);

        if (!result.IsSuccess)
        {
            return result;
        }

        return (Result)await _interactions.EditOriginalInteractionResponseAsync
        (
            _context.Interaction.ApplicationID,
            _context.Interaction.Token,
            "I've recorded your note."
        );
    }

    [Command("unban")]
    [Description("Un-bans a user on the guild.")]
    public async Task<Result> UnbanAsync
    (
        [Description("The user to be unmuted.")]
        [EnsureHierarchy(HierarchyTarget.Self, HierarchyLevel.Higher)]
        [EnsureHierarchy(HierarchyTarget.Invoker, HierarchyLevel.Higher)]
        IUser target,

        [Description("The reason for the unmute.")]
        string reason = "Not Given."
    )
    {
        var result = await _apiService.AddUserUnbanAsync(_context.Interaction.GuildID.Value, target, _context.Interaction.Member.Value.User.Value, reason);

        if (!result.IsSuccess)
        {
            return result;
        }

        return (Result)await _interactions.EditOriginalInteractionResponseAsync
        (
            _context.Interaction.ApplicationID,
            _context.Interaction.Token,
            "User un-banned successfully."
        );
    }

    [Command("unmute")]
    [Description("Un-mutes a user on the guild.")]
    public async Task<Result> UnmuteAsync
    (
        [Description("The user to be unmuted.")]
        [EnsureHierarchy(HierarchyTarget.Self, HierarchyLevel.Higher)]
        [EnsureHierarchy(HierarchyTarget.Invoker, HierarchyLevel.Higher)]
        IUser target,

        [Description("The reason for the unmute.")]
        string reason = "Not Given."
    )
    {
        var result = await _apiService.UnmuteAsync(_context.Interaction.GuildID.Value, target, _context.Interaction.Member.Value.User.Value, reason);

        if (!result.IsSuccess)
        {
            return result;
        }

        return (Result)await _interactions.EditOriginalInteractionResponseAsync
        (
            _context.Interaction.ApplicationID,
            _context.Interaction.Token,
            "User un-muted successfully."
        );
    }

    [Command("pardon")]
    [Description("Pardons a user from an infraction.")]
    public async Task<Result> PardonAsync
    (
        [Option("case_id")]
        [Description("The ID of the case to pardon.")]
        int caseID, // TODO: Autocomplete?

        [Description("The reason for the pardon.")]
        string reason = "Not Given."
    )
    {
        var result = await _apiService.PardonAsync(_context.Interaction.GuildID.Value, _context.Interaction.Member.Value.User.Value, reason, caseID);

        if (!result.IsSuccess)
        {
            return result;
        }

        return (Result)await _interactions.EditOriginalInteractionResponseAsync
        (
            _context.Interaction.ApplicationID,
            _context.Interaction.Token,
            "User un-muted successfully."
        );
    }
}
