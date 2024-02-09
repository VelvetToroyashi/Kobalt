using System.ComponentModel;
using Humanizer;
using Kobalt.Bot.Services;
using Kobalt.Infrastructure;
using Kobalt.Shared.Conditions;
using Kobalt.Shared.Extensions;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Pagination.Extensions;
using Remora.Rest.Core;
using Color = System.Drawing.Color;

namespace Kobalt.Bot.Commands;

[Group("moderation")]
[SkipAssemblyDiscovery]
[RequireContext(ChannelContext.Guild)]
[RequireDiscordPermission(DiscordPermission.ManageRoles, DiscordPermission.KickMembers)]
[RequireBotDiscordPermissions(DiscordPermission.BanMembers, DiscordPermission.ModerateMembers)]
public class ModerationCommands
(
    FeedbackService feedback,
    IDiscordRestUserAPI users,
    IInteractionContext context,
    InfractionAPIService apiService,
    IDiscordRestInteractionAPI interactions
) : CommandGroup
{
    private const string UnknownUser = "@unknown.user";
    private const string UnknownUserID = "000000000000000000";


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
        var result = await apiService.AddUserKickAsync(context.Interaction.GuildID.Value, target, context.Interaction.Member.Value.User.Value, reason);

        if (!result.IsSuccess)
        {
            return result;
        }

        return (Result)await interactions.EditOriginalInteractionResponseAsync
        (
            context.Interaction.ApplicationID,
            context.Interaction.Token,
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
        var result = await apiService.AddUserBanAsync(context.Interaction.GuildID.Value, target, context.Interaction.Member.Value.User.Value, reason, duration, delete);

        if (!result.IsSuccess)
        {
            return result;
        }

        return (Result)await interactions.EditOriginalInteractionResponseAsync
        (
            context.Interaction.ApplicationID,
            context.Interaction.Token,
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
        var result = await apiService.AddUserMuteAsync(context.Interaction.GuildID.Value, target, context.Interaction.Member.Value.User.Value, reason, duration);

        if (!result.IsSuccess)
        {
            return result;
        }

        return (Result)await interactions.EditOriginalInteractionResponseAsync
        (
            context.Interaction.ApplicationID,
            context.Interaction.Token,
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
        var result = await apiService.AddUserStrikeAsync(context.Interaction.GuildID.Value, target, context.Interaction.Member.Value.User.Value, reason);

        if (!result.IsSuccess)
        {
            return result;
        }

        return (Result)await interactions.EditOriginalInteractionResponseAsync
        (
            context.Interaction.ApplicationID,
            context.Interaction.Token,
            "User warned successfully."
        );
    }
    [Command("note")]
    [Description("Sets a note on the user's record.")]
    public async Task<Result> NoteAsync
    (
        [Description("The user to add a note for.")]
        [EnsureHierarchy(HierarchyTarget.Self, HierarchyLevel.Higher)]
        [EnsureHierarchy(HierarchyTarget.Invoker, HierarchyLevel.Higher)]
        IUser target,

        [Description("The reason for the note.")]
        string reason = "Not Given.",

        [Description("The ID of the case to attach this note to.")]
        int? caseID = null
    )
    {
        var result = await apiService.AddUserNoteAsync(context.Interaction.GuildID.Value, target, context.Interaction.Member.Value.User.Value, reason);

        if (!result.IsSuccess)
        {
            return result;
        }
        return (Result)await interactions.EditOriginalInteractionResponseAsync
        (
            context.Interaction.ApplicationID,
            context.Interaction.Token,
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
        var result = await apiService.AddUserUnbanAsync(context.Interaction.GuildID.Value, target, context.Interaction.Member.Value.User.Value, reason);

        if (!result.IsSuccess)
        {
            return result;
        }
        return (Result)await interactions.EditOriginalInteractionResponseAsync
        (
            context.Interaction.ApplicationID,
            context.Interaction.Token,
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
        var result = await apiService.UnmuteAsync(context.Interaction.GuildID.Value, target, context.Interaction.Member.Value.User.Value, reason);

        if (!result.IsSuccess)
        {
            return result;
        }
        return (Result)await interactions.EditOriginalInteractionResponseAsync
        (
            context.Interaction.ApplicationID,
            context.Interaction.Token,
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
        var result = await apiService.PardonAsync(context.Interaction.GuildID.Value, context.Interaction.Member.Value.User.Value, reason, caseID);

        if (!result.IsSuccess)
        {
            return result;
        }
        return (Result)await interactions.EditOriginalInteractionResponseAsync
        (
            context.Interaction.ApplicationID,
            context.Interaction.Token,
            "User un-muted successfully."
        );
    }
    [Command("view_case")]
    [Description("View a specific case.")]
    public async Task<Result> ViewCaseAsync
    (
        [Option("case_id")]
        [Description("The ID of the case to view.")]
        int caseID
    )
    {
        var result = await apiService.GetUserCaseAsync(context.Interaction.GuildID.Value, caseID);

        if (!result.IsDefined(out var infraction))
        {
            return (Result)await interactions.EditOriginalInteractionResponseAsync
            (
                context.Interaction.ApplicationID,
                context.Interaction.Token,
                "I couldn't find that case."
            );
        }
        var moderatorResult = await users.GetUserAsync(new Snowflake(infraction.ModeratorID));
        var targetResult = await users.GetUserAsync(new Snowflake(infraction.UserID));

        var (moderatorID, moderatorTag) = moderatorResult.IsDefined(out var moderator)
            ? (moderator.ID.ToString(), moderator.DiscordTag())
            : (UnknownUserID, UnknownUser);

        var (targetID, targetTag) = targetResult.IsDefined(out var target)
            ? (target.ID.ToString(), target.DiscordTag())
            : (UnknownUserID, UnknownUser);

        var embed = new Embed
        {
            Title = $"Case #{caseID} | {moderatorTag} ➜ {targetTag}",
            Description = infraction.Reason,
            Colour = Color.Goldenrod,
            Fields = new EmbedField[]
            {
                new("Type", infraction.Type.Humanize(), true),
                new("Moderator", $"{moderatorTag}\n`{moderatorID}`", true),
                new("Target", $"{targetTag}\n`{targetID}`", true),
                new("Created", infraction.CreatedAt.ToTimestamp(), true),
                new("Expires", infraction.ExpiresAt?.ToTimestamp() ?? "Never", true),
                new("Hidden", infraction.IsHidden.ToString(), true),
                new("Reference Case", infraction.ReferencedId?.ToString() ?? "None", true),
            }
        };

        return (Result)await interactions.EditOriginalInteractionResponseAsync
        (
            context.Interaction.ApplicationID,
            context.Interaction.Token,
            embeds: new[] { embed }
        );
    }
    [Command("view_cases")]
    [Description("View a user's cases.")]
    public async Task<Result> ViewCasesAsync
    (
        [Option("user")]
        [Description("The user to view cases for.")]
        IUser target,
                
        [Option("include_pardons")]
        [Description("Whether to include padons, unbans, and unmutes in the log.")]
        bool includePardons
    )
    {
        var result = await apiService.GetUserCasesAsync(context.Interaction.GuildID.Value, target.ID, includePardons);

        if (!result.IsDefined(out var infractions))
        {
            return (Result)await interactions.EditOriginalInteractionResponseAsync
            (
                context.Interaction.ApplicationID,
                context.Interaction.Token,
                "That user's history is squeaky clean."
            );
        }
        var embeds = infractions
                    .Select(inf => $"`#{PadLeft(inf.Id)}` | `{inf.Type,-8}` | {inf.Reason.Truncate(50, "[...]")}")
                    .Chunk(15)
                    .Select
                     (
                         chunk => new Embed
                         {
                             Title = $"Cases for {target.DiscordTag()}",
                             Description = string.Join("\n", chunk),
                             Colour = Color.Goldenrod
                         }
                     )
                    .ToArray();

        if (embeds.Length is 1)
        {
            return (Result)await interactions.EditOriginalInteractionResponseAsync
            (
                context.Interaction.ApplicationID,
                context.Interaction.Token,
                embeds: embeds
            );
        }
        return (Result)await feedback.SendPaginatedMessageAsync
        (
            context.Interaction.Channel.Value.ID.Value,
            context.Interaction.Member.Map(m => m.User).OrDefault(context.Interaction.User).Value.ID,
            embeds
        );

        string PadLeft(int value)
        {
            var strVal = value.ToString();
            var biggest = infractions!.Max(inf => inf.Id).ToString();

            return strVal.PadLeft(biggest.Length);
        }
    }
    // TODO: case-update
}
