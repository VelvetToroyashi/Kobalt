using System.Text.Json;
using Kobalt.Shared.DTOs.Reminders;
using Kobalt.Shared.Extensions;
using Kobalt.Shared.Services;
using MassTransit;
using Microsoft.Extensions.Options;
using Polly;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Bot.Services;

public class ReminderAPIService : IConsumer<ReminderDTO>
{
    private readonly IKobaltRestRemindersAPI _reminders;
    private readonly IDiscordRestChannelAPI _channels;

    public ReminderAPIService
    (
        IKobaltRestRemindersAPI reminders,
        IDiscordRestChannelAPI channels
    )
    {
        _reminders = reminders;
        _channels = channels;
    }

    /// <summary>
    /// Creates a reminder.
    /// </summary>
    /// <param name="userID">The ID of the user.</param>
    /// <param name="channelID">The ID of the channel the reminder was created in.</param>
    /// <param name="GuildID">The ID of the guild the reminder was created in.</param>
    /// <param name="content">The content of the reminder.</param>
    /// <param name="replyID">The ID of the message the reminder is replying to, if any.</param>
    /// <param name="expiration">When the reminder expires.</param>
    /// <returns>The ID of the created reminder.</returns>
    public async Task<Result<int>> CreateReminderAsync
    (
        Snowflake userID,
        Snowflake channelID,
        Snowflake? GuildID,
        string content,
        Snowflake? replyID,
        DateTimeOffset expiration
    )
    {
        var payload = new ReminderCreatePayload(channelID.Value, GuildID?.Value, content, expiration, replyID?.Value);
        var response = await ResultExtensions.TryCatchAsync(() => _reminders.CreateReminderAsync(userID, payload));

        if (!response.IsDefined(out var createdReminder))
        {
            return Result<int>.FromError(response.Error);
        }

        return createdReminder.Id;
    }

    /// <summary>
    /// Gets reminders for a specific user.
    /// </summary>
    /// <param name="userID">The ID of the user to fetch reminders for.</param>
    public async Task<Result<IReadOnlyList<ReminderDTO>>> GetRemindersAsync(Snowflake userID)
    {
        var response = await ResultExtensions.TryCatchAsync(() => _reminders.GetRemindersAsync(userID));

        return response;
    }

    /// <summary>
    /// Removes reminders.
    /// </summary>
    /// <param name="userID">The ID of the user./param>
    /// <param name="reminderIDs">The ID of the reminders.</param>
    /// <returns></returns>
    public async Task<Result> DeleteRemindersAsync(Snowflake userID, int[] reminderIDs)
    {
        var response = await ResultExtensions.TryCatchAsync(() => _reminders.DeleteRemindersAsync(userID, reminderIDs));

        return response;
    }

    private async Task DispatchAsync(ReminderDTO reminder, CancellationToken ct)
    {
        var isPrivate = reminder.GuildID is null;
        var channel = await _channels.GetChannelAsync(reminder.ChannelID, ct);

        if (!channel.IsSuccess)
        {
            // TODO: Log
            return;
        }

        string message;

        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
        if (isPrivate)
        {
            message = $"Hey, <t:{reminder.Expiration.ToUnixTimeSeconds()}:R> you asked me remind you:\n {reminder.ReminderContent}";
        }
        else
        {
            message = $"Hey <@{reminder.AuthorID}>, <t:{reminder.Expiration.ToUnixTimeSeconds()}:R> you asked me remind you:\n {reminder.ReminderContent}";
        }

        var sendResult = await _channels.CreateMessageAsync
        (
            reminder.ChannelID,
            message,
            allowedMentions: new AllowedMentions(default, default, new[] { reminder.AuthorID }, false),
            ct: ct
        );

        _ = sendResult;
    }

    public Task Consume(ConsumeContext<ReminderDTO> context) => DispatchAsync(context.Message, context.CancellationToken);
}
