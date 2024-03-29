﻿using System.ComponentModel;
using Humanizer;
using Kobalt.Bot.Services;
using Kobalt.Infrastructure;
using Kobalt.Shared.DTOs.Reminders;
using NodaTime;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Builders;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Interactivity;
using Remora.Discord.Pagination.Extensions;
using Remora.Rest.Core;
using Remora.Results;
using Color = System.Drawing.Color;
using IResult = Remora.Results.IResult;

namespace Kobalt.Bot.Commands;

[SkipAssemblyDiscovery]
public class ReminderContextCommands(IDiscordRestInteractionAPI interactions, IInteractionContext context) : CommandGroup
{
    public const string ReminderModalID = "reminder-modal";

    [Command("Remind me about...")]
    [SuppressInteractionResponse(true)]
    [CommandType(ApplicationCommandType.Message)]
    // TODO: Let this be user installable // [DiscordInstallContext(ApplicationIntegrationType.UserInstallable, ApplicationIntegrationType.GuildInstallable)]
    public async Task<Result> RemindContextMenuAsync(IMessage message)
    {
        return await interactions.CreateInteractionResponseAsync
        (
            context.Interaction.ID,
            context.Interaction.Token,
            new InteractionResponse
            (
                InteractionCallbackType.Modal,
                new
                (
                    new InteractionModalCallbackData
                    (
                        CustomIDHelpers.CreateModalIDWithState(ReminderModalID, message.ID.ToString()),
                        "Set a reminder",
                        new IMessageComponent[] 
                        { 
                            new ActionRowComponent(new[] { new TextInputComponent("reminderTime", TextInputStyle.Short, "In (when) [e.g. In 10 minutes]...", default, 100, true, default, "10 minutes, 10m, tomorrow at 5pm, etc.") }),
                            new ActionRowComponent(new[] { new TextInputComponent("reminderContent", TextInputStyle.Paragraph, "About (what)...", default, 1800, false, default, "What do you want me to remind you about?") }),
                        }
                    )
                )
            )
        );
    }
}

[Group("reminder")]
[SkipAssemblyDiscovery]
public class ReminderCommands : CommandGroup
{
    
    private readonly FeedbackService _feedback;
    private readonly IInteractionContext _context;
    private readonly ReminderAPIService _reminders;
    private readonly IDiscordRestInteractionAPI _interactions;
    
    public ReminderCommands
    (
        FeedbackService feedback,
        IInteractionContext context,
        ReminderAPIService reminders,
        IDiscordRestInteractionAPI interactions
    )
    {
        _feedback = feedback;
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

        var guildId = _context.TryGetGuildID(out var gid) ? (Snowflake?)gid : null;
        
        var expiration = when.Match
        (
            zdt => zdt.ToDateTimeOffset(),
            ts => DateTimeOffset.UtcNow + ts
        );

        var result = await _reminders.CreateReminderAsync(userId, channelId, guildId, of, null, expiration);

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
        if (!_context.TryGetUserID(out var userId))
        {
            return new InvalidOperationError("Could not get user ID.");
        }
        
        var remindersResult = await _reminders.GetRemindersAsync(userId);
        
        if (!remindersResult.IsDefined(out var reminders))
        {
            return (Result)remindersResult;
        }

        if (!reminders.Any())
        {
            return (Result)await _interactions.EditOriginalInteractionResponseAsync
            (
                _context.Interaction.ApplicationID, 
                _context.Interaction.Token,
                "You don't have any reminders."
            );
        }

        // TODO: Avoid pagination if it would be a single page
        var pages = reminders.Chunk(10)
                             .Select
                             (
                                 page => new Embed
                                 {
                                     Title = "Active Reminders",
                                     Colour = Color.DodgerBlue,
                                     Description = GetPageContent(page)
                                 }
                             )
                             .ToArray();
        
        return (Result)await _feedback.SendContextualPaginatedMessageAsync(userId, pages);

        string GetPageContent(IEnumerable<ReminderDTO> pageContent)
        {
            return string.Join
            (
                "\n",
                pageContent.Select
                (
                    r =>
                    {
                        var replyTo = r.ReplyMessageID is null
                            ? null
                            : $" (replying to https://discord.com/channels/{r.GuildID?.ToString() ?? "@me"}{r.ChannelID}/{r.ReplyMessageID} )";

                        return $"`{r.Id}` ➜ <t:{r.Expiration.ToUnixTimeSeconds()}:R>:{replyTo} {r.ReminderContent.Truncate(75, "[...]")}";
                    }
                )
            );
        }
    }

    [Command("delete")]
    [Description("I'll delete a reminder.")]
    public async Task<Result> DeleteAsync
    (
        [AutocompleteProvider("Plugins:Reminders")]
        int id
    )
    {
        if (!_context.TryGetUserID(out var userId))
        {
            return new InvalidOperationError("Could not get user ID.");
        }
        
        var result = await _reminders.DeleteRemindersAsync(userId, new [] { id });

        if (!result.IsSuccess)
        {
            return (Result)await _interactions
            .EditOriginalInteractionResponseAsync
            (
                _context.Interaction.ApplicationID,
                _context.Interaction.Token,
                "I don't see a reminder by that ID, sorry."
            );
        }

        return (Result)await _interactions.EditOriginalInteractionResponseAsync
        (
            _context.Interaction.ApplicationID, 
            _context.Interaction.Token,
            "Consider it gone."
        );
    }
}
