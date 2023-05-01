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

internal record InfractionResult(InfractionDTO Infraction, InfractionState State);

internal enum InfractionState { Created, Updated }

public class InfractionAPIService : IConsumer<InfractionDTO>
{
    private readonly IUser _self;
    private readonly HttpClient _client;
    private readonly IDiscordRestUserAPI _users;
    private readonly IDiscordRestGuildAPI _guilds;
    private readonly IChannelLoggerService _channelLogger;
    private readonly IDiscordRestChannelAPI _channels;
    private readonly JsonSerializerOptions _serializerOptions;

    private static readonly TimeSpan MaxMuteDuration = TimeSpan.FromDays(28);

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
    public async Task<Result> AddUserKickAsync(Snowflake guildID, IUser user, IUser moderator, string reason)
    {
        var infractionResult = await SendInfractionAsync(guildID, user.ID, moderator.ID, reason, InfractionType.Kick);

        if (!infractionResult.IsSuccess)
        {
            return Result.FromError(infractionResult.Error);
        }

        var kickResult = await _guilds.RemoveGuildMemberAsync(guildID, user.ID, reason.Truncate(500));

        if (!kickResult.IsSuccess)
        {
            kickResult = new InvalidOperationError("Failed to kick. Are they still in the server?");
        }

        var embed = GenerateEmbedForInfraction(infractionResult.Entity, user, moderator);

        await _channelLogger.LogAsync(guildID, LogChannelType.CaseCreate, default, new[] { embed });

        return kickResult;
    }

    /// <summary>
    /// Bans a user from a guild.
    /// </summary>
    /// <param name="guildID">The ID of the guild.</param>
    /// <param name="user">The user to be banned.</param>
    /// <param name="moderator">The moderator responsible.</param>
    /// <param name="reason">The reason they're being banned.</param>
    /// <param name="duration">Optionally, how long </param>
    /// <param name="period">Optionally, the period of messages to be deleted.</param>
    /// <returns>A result that may or not be successful.</returns>
    public async Task<Result> AddUserBanAsync(Snowflake guildID, IUser user, IUser moderator, string reason, TimeSpan? duration = null, TimeSpan? period = null)
    {
        var infractionResult = await SendInfractionAsync(guildID, user.ID, moderator.ID, reason, InfractionType.Ban, duration);

        if (!infractionResult.IsSuccess)
        {
            return Result.FromError(infractionResult.Error);
        }

        var banResult = await _guilds.CreateGuildBanAsync(guildID, user.ID, (Optional<int>)(int?)period?.TotalSeconds, reason.Truncate(500));

        if (!banResult.IsSuccess)
        {
            banResult = new InvalidOperationError("Failed to ban. Are they still in the server?");
        }

        var embed = GenerateEmbedForInfraction(infractionResult.Entity, user, moderator);

        await _channelLogger.LogAsync(guildID, LogChannelType.CaseCreate, default, new[] { embed });

        return banResult;
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
    public async Task<Result> AddUserMuteAsync(Snowflake guildID, IUser user, IUser moderator, string reason, TimeSpan duration)
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
            reason: reason.Truncate(500)
        );

        var embed = GenerateEmbedForInfraction(infractionResult.Entity, user, moderator);

        await _channelLogger.LogAsync(guildID, LogChannelType.CaseCreate, default, new[] { embed });

        if (!muteRoleResult.IsSuccess)
        {
            muteRoleResult = new InvalidOperationError("I couldn't mute them; it's possible they've weaseled out. I'll mute them if and when they rejoin.");
        }

        return muteRoleResult;
    }

    /// <summary>
    /// Strikes (or warns) a user in a guild.
    /// </summary>
    /// <param name="guildID">The ID of the guild the warning is being issued in.</param>
    /// <param name="user">The user being warned.</param>
    /// <param name="moderator">The moderator responsible for warning the user.</param>
    /// <param name="reason">The reason the user is being warned.</param>
    /// <returns></returns>
    public async Task<Result> AddUserStrikeAsync(Snowflake guildID, IUser user, IUser moderator, string reason)
    {
        var infractionResult = await SendInfractionAsync(guildID, user.ID, moderator.ID, reason, InfractionType.Warning);

        if (!infractionResult.IsSuccess)
        {
            return Result.FromError(infractionResult.Error);
        }

        var embed = GenerateEmbedForInfraction(infractionResult.Entity, user, moderator);

        await _channelLogger.LogAsync(guildID, LogChannelType.CaseCreate, default, new[] { embed });

        return Result.FromSuccess();
    }

    /// <summary>
    /// Adds a note to a user, optionally attatching it to a case.
    /// </summary>
    /// <param name="guildID">The ID of the guild the note is being issued in.</param>
    /// <param name="user">The user being noted.</param>
    /// <param name="moderator">The moderator responsible for noting the user.</param>
    /// <param name="reason">The reason of the note.</param>
    /// <param name="caseID">The ID of the case this note refers to.</param>
    public async Task<Result> AddUserNoteAsync(Snowflake guildID, IUser user, IUser moderator, string reason, int? caseID = null)
    {
        var infractionResult = await SendInfractionAsync(guildID, user.ID, moderator.ID, reason, InfractionType.Note, null, caseID);

        if (!infractionResult.IsSuccess)
        {
            return Result.FromError(infractionResult.Error);
        }

        var embed = GenerateEmbedForInfraction(infractionResult.Entity, user, moderator);

        await _channelLogger.LogAsync(guildID, LogChannelType.CaseCreate, default, new[] { embed });

        return Result.FromSuccess();
    }

    /// <summary>
    /// Unbans a user in a guild.
    /// </summary>
    /// <param name="guildID">The ID of the guild the unbaning is being issued in.</param>
    /// <param name="user">The user being unbaned.</param>
    /// <param name="moderator">The moderator responsible for unbaning the user.</param>
    /// <param name="reason">The reason the user is being unbaned.</param>
    public async Task<Result> AddUserUnbanAsync(Snowflake guildID, IUser user, IUser moderator, string reason)
    {
        var unbanResult = await _guilds.RemoveGuildBanAsync(guildID, user.ID, reason.Truncate(500));

        if (!unbanResult.IsSuccess)
        {
            return Result.FromError(new InvalidOperationError("That user isn't banned or doesn't exist."));
        }

        var infractionResult = await SendInfractionAsync(guildID, user.ID, moderator.ID, reason, InfractionType.Unban);

        if (!infractionResult.IsSuccess)
        {
            return Result.FromError(infractionResult.Error);
        }

        var embed = GenerateEmbedForInfraction(infractionResult.Entity, user, moderator);

        await _channelLogger.LogAsync(guildID, LogChannelType.CaseCreate, default, new[] { embed });

        return Result.FromSuccess();
    }

    /// <summary>
    /// unmute a user in a guild.
    /// </summary>
    /// <param name="guildID">The ID of the guild the unmuteing is being issued in.</param>
    /// <param name="user">The user being unmuteed.</param>
    /// <param name="moderator">The moderator responsible for unmuteing the user.</param>
    /// <param name="reason">The reason the user is being unmuteed.</param>
    /// <returns></returns>
    public async Task<Result> UnmuteAsync(Snowflake guildID, IUser user, IUser moderator, string reason)
    {
        var infractionResult = await SendInfractionAsync(guildID, user.ID, moderator.ID, reason, InfractionType.Unmute);

        if (!infractionResult.IsSuccess)
        {
            return Result.FromError(infractionResult.Error);
        }

        await _guilds.ModifyGuildMemberAsync
        (
            guildID,
            user.ID,
            communicationDisabledUntil: null,
            reason: reason.Truncate(500)
        );

        var embed = GenerateEmbedForInfraction(infractionResult.Entity, user, moderator);

        await _channelLogger.LogAsync(guildID, LogChannelType.CaseCreate, default, new[] { embed });

        return Result.FromSuccess();
    }

    /// <summary>
    /// Pardons a user in a guild.
    /// </summary>
    /// <param name="guildID">The ID of the guild the pardon is being issued in.</param>
    /// <param name="moderator">The moderator responsible for pardoning the user.</param>
    /// <param name="reason">The reason the user is being pardoned.</param>
    /// <param name="caseID">The ID of the case beign pardoned.</param>
    public async Task<Result> PardonAsync(Snowflake guildID, IUser moderator, string reason, int caseID)
    {
        var getInfractionResult = await ResultExtensions.TryCatchAsync
        (
            () => _client.GetFromJsonAsync<InfractionDTO>($"infractions/guilds/{guildID}/{caseID}", _serializerOptions)
        );

        if (!getInfractionResult.IsDefined(out var fetched))
        {
            return new NotFoundError("That case doesn't exist.");
        }

        var userResult = await _users.GetUserAsync(new Snowflake(fetched.UserID));

        if (!userResult.IsSuccess)
        {
            return Result.FromError(userResult.Error);
        }

        var infractionResult = await SendInfractionAsync(guildID, userResult.Entity.ID, moderator.ID, reason, InfractionType.Pardon, null, caseID);

        if (!infractionResult.IsSuccess)
        {
            return Result.FromError(infractionResult.Error);
        }

        var embed = GenerateEmbedForInfraction(infractionResult.Entity, userResult.Entity, moderator);

        await _channelLogger.LogAsync(guildID, LogChannelType.CaseCreate, default, new[] { embed });

        return Result.FromSuccess();
    }

    /// <summary>
    /// Attempts to escalate an infraction for a user in a guild, using the Guild's configured rules.
    /// </summary>
    /// <param name="guildID">The ID of the guild to escalate. </param>
    /// <param name="user">The user to escalate the infraction for.</param>
    /// <remarks>
    /// This method only *attmepts* to escalate an infraction, automatically handling invoking the correct method
    /// if a rule matches (e.g. three mutes in an hour ➜ ban). If no rule matches, this method will return a successful
    /// content, as the API returns 204 No Content if no rule matches.
    /// </remarks>
    public async Task<Result> TryEscalateInfractionAsync(Snowflake guildID, IUser user)
    {
        using var rulesResult = await _client.PostAsync($"/infractions/guilds/{guildID}/rules/evaluate/{user.ID}", null);

        if (rulesResult.StatusCode is HttpStatusCode.NoContent)
        {
            return Result.FromSuccess();
        }
        else if (rulesResult.StatusCode is not HttpStatusCode.OK)
        {
            return Result.FromError(new InvalidOperationError("Failed to escalate infraction."));
        }
        else
        {
            var match = await rulesResult.Content.ReadFromJsonAsync<InfractionRuleMatch>();

            var res = await (match!.Type switch
            {
                InfractionType.Kick => AddUserKickAsync(guildID, user, _self, "Automatic case esclation."),
                InfractionType.Ban  => AddUserBanAsync(guildID, user, _self, "Automatic case esclation.", match.Duration),
                InfractionType.Mute => AddUserMuteAsync(guildID, user, _self, "Automatic case esclation.", match.Duration.Value),
                _ => Task.FromResult(Result.FromError(new InvalidOperationError($"Unexpected infraction type: {match.Type}")))
            });

            return res;
        }
    }

    private Embed GenerateEmbedForInfraction(InfractionResult result, IUser user, IUser moderator)
    {
        var (infraction, state) = result;
        var title = $"Case {infraction.Id} | {moderator.Username}#{moderator.Discriminator:0000} ➜ {user.Username}#{user.Discriminator:0000}";

        var fieldsList = new List<EmbedField>
        {
            new("Type", infraction.Type.Humanize(), true),
            new("Moderator", $"{moderator.DiscordTag()}\n`{moderator.ID}`", true),
            new("Target", $"{user.DiscordTag()}\n`{user.ID}`", true),
            new("Created", infraction.CreatedAt.ToTimestamp(), true),
        };

        if (infraction.ExpiresAt.HasValue)
        {
            fieldsList.Add(new EmbedField("Expires", infraction.ExpiresAt.Value.ToTimestamp(), true));
        }

        if (infraction.ReferencedId is {} referencedId)
        {
            fieldsList.Add(new EmbedField("Referenced Case", $"`#{referencedId}`", true));
        }

        if (state is InfractionState.Updated)
        {
            fieldsList.Add(new EmbedField("Updated", DateTimeOffset.UtcNow.ToTimestamp(), true));
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

    private async Task<Result<InfractionResult>> SendInfractionAsync
    (
        Snowflake guildID,
        Snowflake userID,
        Snowflake moderatorID,
        string reason,
        InfractionType type,
        TimeSpan? duration = null,
        int? referencedCaseID = null
    )
    {
        var payload = new InfractionCreatePayload(reason, userID.Value, moderatorID.Value, referencedCaseID, type, DateTimeOffset.UtcNow + duration);

        var response = await _client.PutAsJsonAsync($"/infractions/guilds/{guildID}", payload, _serializerOptions);

        if (!response.IsSuccessStatusCode)
        {
            return new HttpRequestException(response.ReasonPhrase);
        }

        var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<InfractionDTO>(stream, _serializerOptions);
        return new InfractionResult(result!, response.StatusCode is HttpStatusCode.Created ? InfractionState.Created : InfractionState.Updated);
    }

    async Task IConsumer<InfractionDTO>.Consume(ConsumeContext<InfractionDTO> context)
    {
        var message = context.Message;

        var getUserResult = await _users.GetUserAsync(new Snowflake(message.UserID));
        var getModeratorResult = await _users.GetUserAsync(new Snowflake(message.ModeratorID));

        if (!getModeratorResult.IsSuccess || !getUserResult.IsSuccess)
        {
            return;
        }

        var embed = GenerateEmbedForInfraction(new InfractionResult(context.Message, InfractionState.Created), getModeratorResult.Entity, getUserResult.Entity);

        await _channelLogger.LogAsync(new Snowflake(message.GuildID), LogChannelType.CaseCreate, default, new[] { embed });
    }
}
