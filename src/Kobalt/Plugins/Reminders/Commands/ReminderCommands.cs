using System.ComponentModel;
using System.Drawing;
using Humanizer;
using Kobalt.Infrastructure.DTOs.Reminders;
using Kobalt.Plugins.Reminders.Services;
using NodaTime;
using OneOf;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Pagination.Extensions;
using Remora.Results;

namespace Kobalt.Plugins.Reminders.Commands;

[Group("reminder")]
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
        if (!_context.TryGetUserID(out var userId))
        {
            return new InvalidOperationError("Could not get user ID.");
        }
        
        var remindersResult = await _reminders.GetRemindersAsync(userId.Value);
        
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
        
        return (Result)await _feedback.SendContextualPaginatedMessageAsync(userId.Value, pages);

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
        
        var result = await _reminders.DeleteRemindersAsync(userId.Value, new [] { id });

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
