﻿using System.ComponentModel;
using NodaTime;
using OneOf;
using ReminderPlugin.Services;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Results;

namespace ReminderPlugin.Commands;

[Group("reminder")]
public class ReminderCommands : CommandGroup
{
    private readonly IInteractionContext _context;
    private readonly ReminderAPIService _reminders;
    private readonly IDiscordRestInteractionAPI _interactions;
    
    public ReminderCommands
    (
        IInteractionContext context,
        ReminderAPIService reminders,
        IDiscordRestInteractionAPI interactions
    )
    {
        _context = context;
        _reminders = reminders;
        _interactions = interactions;
    }

    [Command("set")]
    [Description("I'll remind you of something. Just tell me when.")]
    public async Task<Result> RemindAsync([DiscordTypeHint(TypeHint.String)] OneOf<ZonedDateTime, TimeSpan> when, string of)
    {
        if (!_context.TryGetUserID(out var userId))
        {
            return new InvalidOperationError("Could not get user ID.");
        }

        if (!_context.TryGetChannelID(out var channelId))
        {
            return new InvalidOperationError("Could not get user ID.");
        }
        
        var guildId = _context.TryGetGuildID(out var gid) ? gid : null;
        
        var expiration = when.Match
        (
            zdt => zdt.ToDateTimeOffset(),
            ts => DateTimeOffset.UtcNow + ts
        );

        var result = await _reminders.CreateReminderAsync(userId.Value, channelId.Value, guildId, of, null, expiration);

        if (!result.IsSuccess)
        {
            return (Result)result;
        }

        var reminder = result.Entity;
        var content = $"Got it; your reminder's ID is `{reminder}`. See you <t:{expiration.ToUnixTimeSeconds()}:R>.";

        return (Result)await _interactions.EditOriginalInteractionResponseAsync(_context.Interaction.ApplicationID, _context.Interaction.Token, content);
    }
    
    [Command("list")]
    [Description("I'll list all your reminders.")]
    public async Task<Result> ListAsync()
    {
        return default;
    }
    
    [Command("delete")]
    [Description("I'll delete a reminder.")]
    public async Task<Result> DeleteAsync([DiscordTypeHint(TypeHint.String)] string id)
    {
        return default;
    }
}
