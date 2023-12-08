using Kobalt.Bot.Commands;
using Kobalt.Bot.Services;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Interactivity;
using Remora.Rest.Core;
using Constants = Remora.Discord.API.Constants;

namespace Kobalt.Bot.Interactivity;

public class ReminderModalHandler
(
    IDiscordRestInteractionAPI interactions, 
    IInteractionContext context, 
    ReminderAPIService reminders
) : InteractionGroup
{
    [Ephemeral]
    [Modal(ReminderContextCommands.ReminderModalID)]
    public async Task<Result> HandleAsync(TimeSpan reminderTime, string reminderContent, string state)
    {
        var message = new Snowflake(ulong.Parse(state));
        _ = context.TryGetUserID(out var userID);
        _ = context.TryGetChannelID(out var channelID);
        var inGuild = context.TryGetGuildID(out var guildID);
        
        var expires = DateTimeOffset.UtcNow + reminderTime;

        var createResult = await reminders.CreateReminderAsync
        (
            userID, 
            channelID, 
            inGuild ? guildID : null, 
            reminderContent, 
            Snowflake.CreateTimestampSnowflake(DateTimeOffset.UtcNow, Constants.DiscordEpoch), 
            expires
        );
        
        if (!createResult.IsSuccess)
        {
            return (Result)createResult;
        }
        
        var reminder = createResult.Entity;
        var content = $"Got it; your reminder's ID is `{reminder}`. See you <t:{expires.ToUnixTimeSeconds()}:R>.";

        return (Result)await interactions.CreateFollowupMessageAsync(context.Interaction.ApplicationID, context.Interaction.Token, content, flags: MessageFlags.Ephemeral);
    }
}