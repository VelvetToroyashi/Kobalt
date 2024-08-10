using System.ComponentModel;
using Kobalt.Bot.Services;
using Kobalt.Shared.Types;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Bot.Commands;

public class PurgeCommand(IInteractionContext context, IDiscordRestInteractionAPI interactions, MessagePurgeService purgeService) : CommandGroup
{
    [Command("purge")]
    [Ephemeral]
    [Description("Expell messages.")]
    [RequireContext(ChannelContext.Guild)]
    [RequireBotDiscordPermissions(DiscordPermission.ManageMessages)]
    [DiscordDefaultMemberPermissions(DiscordPermission.ManageMessages)]
    public async Task<Result> PurgeMessagesAsync
    (
        [MinValue(1)]
        [Description("How many messages to purge.")]
        int amount,

        [Description("What channel to purge; ignored if user is specified.")]
        IChannel? channel = null,

        [Description("User to purge messages from, across all channels.")]
        IUser? user = null,

        [Description("Regex to filter by; no effect if user is specified.")]
        string regex = "",

        [DiscordTypeHint(TypeHint.String)]
        OneOf<Snowflake, IPartialMessage>? around = null,

        [DiscordTypeHint(TypeHint.String)]
        OneOf<Snowflake, IPartialMessage>? before = null,

        [DiscordTypeHint(TypeHint.String)]
        OneOf<Snowflake, IPartialMessage>? after = null,

        string reason = "Not given."
    )
    {
        Result<int> result;

        if (user is not null)
        {
            result = await purgeService.PurgeByUserAsync(context.Interaction.GuildID.Value, user.ID, amount, reason);
        }
        else if (!string.IsNullOrEmpty(regex))
        {
            result = await purgeService.PurgeByRegexAsync(channel?.ID ?? context.Interaction.Channel.Value.ID.Value, amount, regex, reason);
        }
        else
        {
            result = await purgeService.PurgeByChannelAsync
            (
                channel?.ID ?? context.Interaction.Channel.Value.ID.Value,
                around?.Match(f0 => f0, f1 => f1.ID.Value),
                before?.Match(f0 => f0, f1 => f1.ID.Value),
                after?.Match(f0 => f0, f1 => f1.ID.Value),
                amount,
                reason
            );
        }

        if (result.IsDefined(out var deleted))
        {
            await interactions.EditOriginalInteractionResponseAsync
            (
                context.Interaction.ApplicationID,
                context.Interaction.Token,
                $"{KobaltEmoji.Trash} Deleted {deleted} messages."
            );
        }
        else
        {
            await interactions.EditOriginalInteractionResponseAsync
            (
                context.Interaction.ApplicationID,
                context.Interaction.Token,
                result.Error!.Message
            );
        }

        return Result.FromSuccess();
    }
}
