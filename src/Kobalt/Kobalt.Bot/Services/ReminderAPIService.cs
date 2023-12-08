using System.Text.Json;
using Kobalt.Shared.DTOs.Reminders;
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
    private readonly HttpClient _client;
    private readonly IDiscordRestChannelAPI _channels;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly IAsyncPolicy<HttpResponseMessage> _policy;

    public ReminderAPIService
    (
        IHttpClientFactory client,
        IDiscordRestChannelAPI channels,
        IOptionsMonitor<JsonSerializerOptions> serializerOptions,
        IAsyncPolicy<HttpResponseMessage> policy
    )
    {
        _client = client.CreateClient("Reminders");
        _channels = channels;
        _serializerOptions = serializerOptions.Get("Discord");
        _policy = policy;
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
        using var response = await _policy.ExecuteAsync(async () => await _client.PostAsJsonAsync($"api/reminders/{userID}", payload, _serializerOptions));

        if (!response.IsSuccessStatusCode)
        {
            return new HttpRequestException(response.ReasonPhrase, null, response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync<ReminderCreationPayload>();

        return result!.Id;
    }

    /// <summary>
    /// Gets reminders for a specific user.
    /// </summary>
    /// <param name="userID">The ID of the user to fetch reminders for.</param>
    public async Task<Result<ReminderDTO[]>> GetRemindersAsync(Snowflake userID)
    {
        using var response = await _policy.ExecuteAsync(async () => await _client.GetAsync($"api/reminders/{userID}"));

        if (!response.IsSuccessStatusCode)
        {
            return new HttpRequestException(response.ReasonPhrase, null, response.StatusCode);
        }

        var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<ReminderDTO[]>(stream, _serializerOptions);

        return result;
    }

    /// <summary>
    /// Removes reminders.
    /// </summary>
    /// <param name="userID">The ID of the user./param>
    /// <param name="reminderIDs">The ID of the reminders.</param>
    /// <returns></returns>
    public async Task<Result> DeleteRemindersAsync(Snowflake userID, int[] reminderIDs)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/reminders/{userID}") { Content = new StringContent(JsonSerializer.Serialize(reminderIDs)) };
        request.Content.Headers.ContentType = new("application/json");
        using var response = await _policy.ExecuteAsync(async () => await _client.SendAsync(request));

        if (!response.IsSuccessStatusCode)
        {
            return new HttpRequestException(response.ReasonPhrase, null, response.StatusCode);
        }

        return Result.FromSuccess();
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
