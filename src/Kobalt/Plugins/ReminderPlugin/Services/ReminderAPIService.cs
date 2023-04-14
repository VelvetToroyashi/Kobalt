using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;
using Kobalt.Infrastructure.DTOs.Reminders;
using Microsoft.Extensions.Options;
using Polly;
using Remora.Rest.Core;
using Remora.Results;

namespace ReminderPlugin.Services;

public class ReminderAPIService
{
    private readonly HttpClient _client;
    private readonly ClientWebSocket _websocket;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly IAsyncPolicy<HttpResponseMessage> _policy;
    
    public ReminderAPIService
    (
        IHttpClientFactory client,
        IOptionsMonitor<JsonSerializerOptions> serializerOptions,
        IAsyncPolicy<HttpResponseMessage> policy
    )
    {
        _client = client.CreateClient("Reminders");
        _websocket = new ClientWebSocket();
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
        using var response = await _policy.ExecuteAsync(async() => await _client.GetAsync($"api/reminders/{userID}"));
        
        if (!response.IsSuccessStatusCode)
        {
            return new HttpRequestException(response.ReasonPhrase, null, response.StatusCode);
        }

        var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<ReminderDTO[]>(stream);

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
        using var response = await _policy.ExecuteAsync(async () => await _client.SendAsync(request));
        
        if (!response.IsSuccessStatusCode)
        {
            return new HttpRequestException(response.ReasonPhrase, null, response.StatusCode);
        }

        return Result.FromSuccess();
    }
}
