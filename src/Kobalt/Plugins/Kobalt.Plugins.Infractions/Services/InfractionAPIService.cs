using System.Drawing;
using System.Net.Http.Json;
using System.Text.Json;
using Humanizer;
using Kobalt.Infractions.Infrastructure.Mediator.DTOs;
using Kobalt.Infractions.Shared;
using Kobalt.Infractions.Shared.Payloads;
using Kobalt.Shared.Extensions;
using Kobalt.Shared.Services;
using Kobalt.Shared.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Plugins.Infractions.Services;

public class InfractionAPIService
{
    private readonly Uri _apiUrl;
    private readonly HttpClient _client;
    private readonly IDiscordRestUserAPI _users;
    private readonly IDiscordRestGuildAPI _guilds;
    private readonly IChannelLoggerService _channelLogger;
    private readonly IDiscordRestChannelAPI _channels;
    private readonly JsonSerializerOptions _serializerOptions;
    
    
    public InfractionAPIService
    (
        IHttpClientFactory client,
        IConfiguration config,
        IDiscordRestUserAPI users,
        IDiscordRestGuildAPI guilds,
        IChannelLoggerService channelLogger,
        IDiscordRestChannelAPI channels,
        IOptionsMonitor<JsonSerializerOptions> serializerOptions
    )
    {
        _apiUrl = new(config["Plugins:Infractions:WebsocketUrl"]!);
        _client = client.CreateClient("Infractions");
        _users = users;
        _guilds = guilds;
        _channelLogger = channelLogger;
        _channels = channels;
        _serializerOptions = serializerOptions.Get("Discord");
    }
    
    /// <summary>
    /// Kicks a user from a guild.
    /// </summary>
    /// <param name="guildID">The ID of the guild.</param>
    /// <param name="user">The user to be kicked.</param>
    /// <param name="moderator">The moderator responsible.</param>
    /// <param name="reason">The reason they're being kicked.</param>
    /// <returns>A result that may or not be successful.</returns>
    public async Task<Result> KickUserAsync(Snowflake guildID, IUser user, IUser moderator, string reason)
    {
        var infractionResult = await SendInfractionAsync(guildID, user.ID, moderator.ID, reason, InfractionType.Kick);
        
        if (!infractionResult.IsSuccess)
            return Result.FromError(infractionResult.Error);
        
        var kickResult = await _guilds.RemoveGuildMemberAsync(guildID, user.ID, reason.Truncate(100));

        if (!kickResult.IsSuccess)
        {
            return new InvalidOperationError("Failed to kick. Are they still in the server?");
        }
        
        var embed = GenerateEmbedForInfraction(infractionResult.Entity, user, moderator);
        
        await _channelLogger.LogAsync(guildID, LogChannelType.CaseCreate, default, new[] { embed });
        
        return Result.FromSuccess();
    }

    private Embed GenerateEmbedForInfraction(InfractionDTO infraction, IUser user, IUser moderator)
    {
        var title = $"Case {infraction.Id} | {moderator.Username}#{moderator.Discriminator:0000} ➜ {user.Username}#{user.Discriminator:0000}";

        var fieldsList = new List<EmbedField>
        {
            new EmbedField("Type", infraction.Type.Humanize(), true),
            new EmbedField("Moderator", $"{moderator.DiscordTag()}\n`{moderator.ID}`", true),
            new EmbedField("Target", $"{user.DiscordTag()}\n`{user.ID}`", true),
            new EmbedField("Created", infraction.CreatedAt.ToTimestamp(), true),
        };

        if (infraction.ExpiresAt.HasValue)
        {
            fieldsList.Add(new EmbedField("Expires", infraction.ExpiresAt.Value.ToTimestamp(), true));
        }
        
        if (infraction.ReferencedId is {} referencedId)
        {
            fieldsList.Add(new EmbedField("Referenced Case", $"`#{referencedId}`", true));
        }

        var embed = new Embed
        (
            Title: title,
            Description: infraction.Reason,
            Colour: infraction.Type switch
            {
                InfractionType.Ban or InfractionType.Kick or InfractionType.Mute => Color.Crimson,
                InfractionType.Warning => Color.Goldenrod,
                InfractionType.Note => Color.Gray,
                InfractionType.Unban => Color.CornflowerBlue,
                InfractionType.Unmute => Color.CornflowerBlue,
                InfractionType.Pardon => Color.CornflowerBlue,
                _ => throw new ArgumentOutOfRangeException()
            },
            Fields: fieldsList
        );
        
        return embed;
    }

    private async Task<Result<InfractionDTO>> SendInfractionAsync
    (
        Snowflake guildID,
        Snowflake userID,
        Snowflake moderatorID,
        string reason,
        InfractionType type
    )
    {
        var payload = new InfractionCreatePayload(reason, userID.Value, moderatorID.Value, null, type, null);

        var response = await _client.PostAsJsonAsync($"{_apiUrl}/guilds/{guildID}/infractions", payload, _serializerOptions);

        if (!response.IsSuccessStatusCode)
        {
            return new HttpRequestException(response.ReasonPhrase);
        }

        var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<InfractionDTO>(stream, _serializerOptions);
        return result;
    }
}
