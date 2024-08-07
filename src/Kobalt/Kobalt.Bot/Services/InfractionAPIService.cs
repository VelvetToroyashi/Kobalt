using System.Net;
using System.Text.Json;
using Humanizer;
using Kobalt.Infractions.Shared;
using Kobalt.Infractions.Shared.DTOs;
using Kobalt.Infractions.Shared.Interfaces;
using Kobalt.Infractions.Shared.Payloads;
using Kobalt.Shared.Extensions;
using Kobalt.Shared.Services;
using Kobalt.Shared.Types;
using MassTransit;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Results;
using Color = System.Drawing.Color;

namespace Kobalt.Bot.Services;

internal record InfractionResult(InfractionResponsePayload Infraction, InfractionState State);

internal enum InfractionState { Created, Updated }

public class InfractionAPIService : IConsumer<InfractionDTO>
{
    private readonly IUser _self;
    private readonly IInfractionAPI _client;
    private readonly IDiscordRestUserAPI _users;
    private readonly IDiscordRestGuildAPI _guilds;
    private readonly IChannelLoggerService _channelLogger;
    private readonly IDiscordRestChannelAPI _channels;

    private static readonly TimeSpan MaxMuteDuration = TimeSpan.FromDays(28);

    public InfractionAPIService
    (
        IUser self,
        IInfractionAPI client,
        IDiscordRestUserAPI users,
        IDiscordRestGuildAPI guilds,
        IChannelLoggerService channelLogger,
        IDiscordRestChannelAPI channels
    )
    {
        _self = self;
        _client = client;
        _users = users;
        _guilds = guilds;
        _channelLogger = channelLogger;
        _channels = channels;
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

        var embed = GenerateEmbedsForInfractions(infractionResult.Entity, user, moderator);

        await _channelLogger.LogAsync(guildID, LogChannelType.CaseCreate, default, new(embed));

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

        var banResult = await _guilds.CreateGuildBanAsync(guildID, user.ID, ((int?)period?.TotalSeconds).AsOptional(), reason.Truncate(500));

        if (!banResult.IsSuccess)
        {
            banResult = new InvalidOperationError("Failed to ban. Are they still in the server?");
        }

        var embed = GenerateEmbedsForInfractions(infractionResult.Entity, user, moderator);

        await _channelLogger.LogAsync(guildID, LogChannelType.CaseCreate, default, new(embed));

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

        var embed = GenerateEmbedsForInfractions(infractionResult.Entity, user, moderator);

        await _channelLogger.LogAsync(guildID, LogChannelType.CaseCreate, default, new(embed));

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

        var embed = GenerateEmbedsForInfractions(infractionResult.Entity, user, moderator);

        await _channelLogger.LogAsync(guildID, LogChannelType.CaseCreate, default, new(embed));

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

        var embed = GenerateEmbedsForInfractions(infractionResult.Entity, user, moderator);

        await _channelLogger.LogAsync(guildID, LogChannelType.CaseCreate, default, new(embed));
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

        var embed = GenerateEmbedsForInfractions(infractionResult.Entity, user, moderator);

        await _channelLogger.LogAsync(guildID, LogChannelType.CaseCreate, default, new(embed));

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

        var embed = GenerateEmbedsForInfractions(infractionResult.Entity, user, moderator);

        await _channelLogger.LogAsync(guildID, LogChannelType.CaseCreate, default, new(embed));

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
            () => _client.GetGuildInfractionAsync(guildID, caseID)
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

        var embed = GenerateEmbedsForInfractions(infractionResult.Entity, userResult.Entity, moderator);

        await _channelLogger.LogAsync(guildID, LogChannelType.CaseCreate, default, new(embed));

        return Result.FromSuccess();
    }

    /// <summary>
    /// Retrieves a singular infraction from a guild.
    /// </summary>
    /// <param name="guildID">The ID of the guild the infraction belongs to.</param>
    /// <param name="caseID">The ID of the case to retrieve.</param>
    /// <returns>The infraction, if it exists.</returns>
    public async Task<Result<InfractionDTO>> GetUserCaseAsync(Snowflake guildID, int caseID)
    {
        var getInfractionResult = await ResultExtensions.TryCatchAsync
        (
            () => _client.GetGuildInfractionAsync(guildID, caseID)
        );

        if (!getInfractionResult.IsDefined(out var fetched))
        {
            return new NotFoundError("That case doesn't exist.");
        }

        return fetched;
    }

    /// <summary>
    /// Retrieves all infractions for a user in a guild.
    /// </summary>
    /// <param name="guildID">The ID of the guild to request infractions for.</param>
    /// <param name="userID">The ID fo the user to request infractions for.</param>
    /// <param name="includePardons"></param>
    /// <returns>The user's infractions, if any.</returns>
    public async Task<Result<IReadOnlyList<InfractionDTO>>> GetUserCasesAsync(Snowflake guildID, Snowflake userID, bool includePardons)
    {
        var getInfractionsResult = await ResultExtensions.TryCatchAsync
        (
            () => _client.GetInfractionsForUserAsync(guildID, userID, includePardons)
        );

        if (!getInfractionsResult.IsDefined(out var fetched) || !fetched.Any())
        {
            return new NotFoundError("That user doesn't have any cases.");
        }

        return getInfractionsResult;
    }

    private IReadOnlyList<Embed> GenerateEmbedsForInfractions(InfractionResult infraction, IUser user, IUser moderator)
    {
        var embeds = new Embed[1 + infraction.Infraction.AdditionalInfractions.Map(i => i.Count).OrDefault(0)];

        var dto = new InfractionDTO
        (
            infraction.Infraction.Id,
            infraction.Infraction.ReferencedId,
            false,
            infraction.Infraction.Reason,
            infraction.Infraction.UserID,
            infraction.Infraction.ModeratorID,
            infraction.Infraction.GuildID,
            infraction.Infraction.Type,
            infraction.Infraction.CreatedAt,
            infraction.Infraction.ExpiresAt
        );

        embeds[0] = GenerateEmbedForInfraction(dto, user, moderator, infraction.Infraction.IsUpdated);

        for (var i = 1; i < embeds.Length; i++)
        {
            embeds[i] = GenerateEmbedForInfraction(infraction.Infraction.AdditionalInfractions.Value[i - 1], user, moderator);
        }

        return embeds;
    }

    private Embed GenerateEmbedForInfraction(InfractionDTO infraction, IUser user, IUser moderator, bool isInfractionUpdate = false)
    {
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

        if (isInfractionUpdate)
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
        var response = await ResultExtensions.TryCatchAsync(() => _client.CreateInfractionAsync(guildID, payload));

        if (!response.IsDefined(out var infraction))
        {
            return Result<InfractionResult>.FromError(response.Error);
        }

        return new InfractionResult(infraction, infraction.IsUpdated ? InfractionState.Created : InfractionState.Updated);
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

        var embed = GenerateEmbedForInfraction(context.Message, getUserResult.Entity, getModeratorResult.Entity);

        await _channelLogger.LogAsync(new Snowflake(message.GuildID), LogChannelType.CaseCreate, default, new[] { embed });
    }
}
