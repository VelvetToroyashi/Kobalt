using System.Drawing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Humanizer;
using Kobalt.Infractions.Infrastructure.Mediator.DTOs;
using Kobalt.Infractions.Shared;
using Kobalt.Infractions.Shared.Payloads;
using Kobalt.Shared.Extensions;
using Kobalt.Shared.Services;
using Kobalt.Shared.Types;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Plugins.Infractions.Services;

public class InfractionAPIService : IConsumer<InfractionDTO>
{
    private readonly Uri _apiUrl;
    private readonly IUser _self;
    private readonly HttpClient _client;
    private readonly IDiscordRestUserAPI _users;
    private readonly IDiscordRestGuildAPI _guilds;
    private readonly IChannelLoggerService _channelLogger;
    private readonly IDiscordRestChannelAPI _channels;
    private readonly JsonSerializerOptions _serializerOptions;

    private readonly static TimeSpan MaxMuteDuration = TimeSpan.FromDays(28);

    public InfractionAPIService
    (
        IUser self,
        IHttpClientFactory client,
        IConfiguration config,
        IDiscordRestUserAPI users,
        IDiscordRestGuildAPI guilds,
        IChannelLoggerService channelLogger,
        IDiscordRestChannelAPI channels,
        IOptionsMonitor<JsonSerializerOptions> serializerOptions
    )
    {
        _self = self;
        _apiUrl = new Uri(config["Plugins:Infractions:WebsocketUrl"]!);
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
        {
            return Result.FromError(infractionResult.Error);
        }

        var kickResult = await _guilds.RemoveGuildMemberAsync(guildID, user.ID, reason.Truncate(100));

        if (!kickResult.IsSuccess)
        {
            return new InvalidOperationError("Failed to kick. Are they still in the server?");
        }

        var embed = GenerateEmbedForInfraction(infractionResult.Entity, user, moderator);

        await _channelLogger.LogAsync(guildID, LogChannelType.CaseCreate, default, new[] { embed });

        await TryEscalateInfractionAsync(guildID, user);
        return Result.FromSuccess();
    }

    /// <summary>
    /// Bans a user from a guild.
    /// </summary>
    /// <param name="guildID">The ID of the guild.</param>
    /// <param name="user">The user to be banned.</param>
    /// <param name="moderator">The moderator responsible.</param>
    /// <param name="reason">The reason they're being banned.</param>
    /// <param name="duration">Optionally, how long </param>
    /// <returns>A result that may or not be successful.</returns>
    public async Task<Result> BanUserAsync(Snowflake guildID, IUser user, IUser moderator, string reason, TimeSpan? duration = null)
    {
        var infractionResult = await SendInfractionAsync(guildID, user.ID, moderator.ID, reason, InfractionType.Ban, duration);

        if (!infractionResult.IsSuccess)
        {
            return Result.FromError(infractionResult.Error);
        }

        var banResult = await _guilds.CreateGuildBanAsync(guildID, user.ID, (Optional<int>)(int?)duration?.TotalSeconds, reason.Truncate(100));

        if (!banResult.IsSuccess)
        {
            return new InvalidOperationError("Failed to ban. Are they still in the server?");
        }

        var embed = GenerateEmbedForInfraction(infractionResult.Entity, user, moderator);

        await _channelLogger.LogAsync(guildID, LogChannelType.CaseCreate, default, new[] { embed });

        await TryEscalateInfractionAsync(guildID, user);

        return Result.FromSuccess();
    }

    /// <summary>
    /// Mutes a user in a guild.
    /// </summary>
    /// <param name="guildID">The ID of the guild.</param>
    /// <param name="user">The user to be muted.</param>
    /// <param name="moderator">The moderator responsible.</param>
    /// <param name="reason">The reason they're being muted.</param>
    /// <param name="duration">How long to mute them for. </param>
    /// <returns></returns>
    public async Task<Result> MuteUserAsync(Snowflake guildID, IUser user, IUser moderator, string reason, TimeSpan duration)
    {
        if (duration > MaxMuteDuration)
        {
            return new InvalidOperationError($"Mute duration cannot exceed {MaxMuteDuration.Humanize()}");
        }

        var infractionResult = await SendInfractionAsync(guildID, user.ID, moderator.ID, reason, InfractionType.Mute, duration);

        if (!infractionResult.IsSuccess)
        {
            return Result.FromError(infractionResult.Error);
        }

        var muteRoleResult = await _guilds.ModifyGuildMemberAsync
        (
            guildID,
            user.ID,
            communicationDisabledUntil: DateTimeOffset.UtcNow + duration,
            reason: reason.Truncate(100)
        );

        var embed = GenerateEmbedForInfraction(infractionResult.Entity, user, moderator);

        await _channelLogger.LogAsync(guildID, LogChannelType.CaseCreate, default, new[] { embed });
        await TryEscalateInfractionAsync(guildID, user);

        if (!muteRoleResult.IsSuccess)
        {
            return new InvalidOperationError("I couldn't mute them; it's possible they've weaseled out. I'll mute them if and when they rejoin.");
        }

        return Result.FromSuccess();
    }

    public async Task<Result> WarnAsync(Snowflake guildID, IUser user, IUser moderator, string reason)
    {
        var infractionResult = await SendInfractionAsync(guildID, user.ID, moderator.ID, reason, InfractionType.Warning);

        if (!infractionResult.IsSuccess)
        {
            return Result.FromError(infractionResult.Error);
        }

        var embed = GenerateEmbedForInfraction(infractionResult.Entity, user, moderator);

        await _channelLogger.LogAsync(guildID, LogChannelType.CaseCreate, default, new[] { embed });

        await TryEscalateInfractionAsync(guildID, user);

        return Result.FromSuccess();
    }

    private async Task<Result> TryEscalateInfractionAsync(Snowflake guildID, IUser user)
    {
        using var rulesResult = await _client.PostAsync($"/infractions/guilds/{guildID}/rules/evaluate/{user.ID}", null);

        if (rulesResult.StatusCode is HttpStatusCode.NoContent)
        {
            return Result.FromSuccess();
        }
        else if (rulesResult.StatusCode is HttpStatusCode.OK)
        {
            var match = await rulesResult.Content.ReadFromJsonAsync<InfractionRuleMatch>();

            var res = await (match!.Type switch
            {
                InfractionType.Kick => KickUserAsync(guildID, user, _self, "Automatic case esclation."),
                InfractionType.Ban  => BanUserAsync(guildID, user, _self, "Automatic case esclation.", match.Duration),
                InfractionType.Mute => MuteUserAsync
                (guildID, user, _self, "Automatic case esclation.", match.Duration.Value),
                _ => Task.FromResult(Result.FromError(new InvalidOperationError($"Unexpected infraction type: {match.Type}")))
            });

            return res;
        }

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
        InfractionType type,
        TimeSpan? duration = null
    )
    {
        var payload = new InfractionCreatePayload(reason, userID.Value, moderatorID.Value, null, type, DateTimeOffset.UtcNow + duration);

        var response = await _client.PutAsJsonAsync($"{_apiUrl}/guilds/{guildID}", payload, _serializerOptions);

        if (!response.IsSuccessStatusCode)
        {
            return new HttpRequestException(response.ReasonPhrase);
        }

        var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<InfractionDTO>(stream, _serializerOptions);
        return result;
    }

    public async Task Consume(ConsumeContext<InfractionDTO> context)
    {
        var message = context.Message;

        var getUserResult = await _users.GetUserAsync(new Snowflake(message.UserID));
        var getModeratorResult = await _users.GetUserAsync(new Snowflake(message.ModeratorID));

        if (!getModeratorResult.IsSuccess || !getUserResult.IsSuccess)
        {
            return;
        }

        var embed = GenerateEmbedForInfraction(context.Message, getModeratorResult.Entity, getUserResult.Entity);

        await _channelLogger.LogAsync(new Snowflake(message.GuildID), LogChannelType.CaseCreate, default, new[] { embed });
    }
}
