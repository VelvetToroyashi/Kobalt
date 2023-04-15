using System.Buffers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;
using Kobalt.Infrastructure.DTOs.Reminders;
using Kobalt.Shared.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Polly;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Results;

namespace ReminderPlugin.Services;

public class ReminderAPIService : BackgroundService
{
    private ClientWebSocket _websocket;
    
    private readonly Uri _apiUrl;
    private readonly HttpClient _client;
    private readonly IDiscordRestUserAPI _users;
    private readonly IDiscordRestChannelAPI _channels;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly IAsyncPolicy<HttpResponseMessage> _policy;
    
    public ReminderAPIService
    (
        IHttpClientFactory client,
        IConfiguration config,
        IDiscordRestUserAPI users,
        IDiscordRestChannelAPI channels,
        IOptionsMonitor<JsonSerializerOptions> serializerOptions,
        IAsyncPolicy<HttpResponseMessage> policy
    )
    {
        _apiUrl = new(config["Plugins:Reminders:WebsocketUrl"]!);
        _client = client.CreateClient("Reminders");
        _users = users;
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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectPolicy = Policy.Handle<WebSocketException>().WaitAndRetryAsync(5, r => TimeSpan.FromSeconds(Math.Pow(2, r)));
        await ReconnectAsync(connectPolicy, stoppingToken);
        
        var buffer = ArrayPool<byte>.Shared.Rent(1024 * 4);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var stream = new MemoryStream();
        
            var result = await ResultExtensions.TryCatchAsync
            (
                async () =>
                {
                    //TODO: Use Microsoft.Toolkit.HighPerformance (see  https://github.com/VelvetThePanda/FurCord.NET/blob/bc0abf5/src/FurCord.NET/Net/Clients/Websocket/WebSocketClient.cs#L129-L148 )
                    var res = await _websocket.ReceiveAsync(new ArraySegment<byte>(buffer), stoppingToken);
                    
                    stream.Write(buffer, 0, res.Count);

                    return res;
                }
            );
            
            if (!result.IsSuccess && !stoppingToken.IsCancellationRequested)
            {
                await ReconnectAsync(connectPolicy, stoppingToken);
            }
            
            if (result.Entity.MessageType == WebSocketMessageType.Close)
            {
                await ReconnectAsync(connectPolicy, stoppingToken);
            }
            
            stream.Seek(0, SeekOrigin.Begin);
            
            var reminder = JsonSerializer.Deserialize<ReminderDTO>(stream, _serializerOptions)!;
            
            await DispatchAsync(reminder, stoppingToken);
        }
        
        ArrayPool<byte>.Shared.Return(buffer, true);
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
            message = $"Hey, <t:{reminder.Expiration.ToUnixTimeSeconds()}:R> you asked me remind you in <#{reminder.ChannelID}>:\n {reminder.ReminderContent}";
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
    
    private async Task ReconnectAsync(AsyncPolicy policy, CancellationToken ct)
    {
        await ResultExtensions.TryCatchAsync(async () => await _websocket?.CloseAsync(WebSocketCloseStatus.NormalClosure, "Reconnecting", CancellationToken.None)!);
        
        _websocket = new ClientWebSocket();
        
        await policy.ExecuteAsync
        (
            async (context, token) =>
            {
                await ((ClientWebSocket)context["socket"]).ConnectAsync((Uri)context["uri"], token);

                return (ClientWebSocket)context["socket"];
            },
            new Context()
            {
                {"socket", _websocket},
                {"uri", _apiUrl}
            },
            ct
        );
    }
}
        
        
        