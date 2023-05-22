using Kobalt.Infractions.Shared;
using Kobalt.Plugins.Core.Data.Entities;
using Kobalt.Plugins.Core.Data.Mediator;
using MediatR;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;

namespace Kobalt.Plugins.Core.Services;

/// <summary>
/// Service for detecting phishing attempts.
/// </summary>
public class PhishingDetectionService
{
    private readonly IUser _self;
    private readonly IMediator _mediator;
    private readonly PhishingAPIService _phishing;
    private readonly InfractionAPIService _infractions;

    public PhishingDetectionService(IUser self, IMediator mediator, PhishingAPIService phishing, InfractionAPIService infractions)
    {
        _self = self;
        _mediator = mediator;
        _phishing = phishing;
        _infractions = infractions;
    }

    /// <summary>
    /// Handles potential phishing in a user's profile
    /// </summary>
    /// <param name="user">The user to handle phishing in.</param>
    /// <param name="guildID">The guild ID to check phishing for.</param>
    /// <param name="ct">A cancellation token.</param>
    public async Task<Result> HandleAsync(IUser user, Snowflake guildID, CancellationToken ct = default)
    {
        var configResult = await _mediator.Send(new GetGuild.PhishingConfigRequest(guildID));

        if (!configResult.IsDefined(out var config) || !config.ScanUsers)
        {
            return (Result)configResult;
        }

        var phishingResult = await _phishing.DetectUserPhishingAsync(guildID, user.ID, user.Username, user.Avatar?.Value);

        if (!phishingResult.Match)
        {
            return Result.FromSuccess();
        }

        return await HandlePhishingAsync(config, user, guildID, phishingResult.DetectionReason!);
    }

    /// <summary>
    /// Handles potential phishing in a message.
    /// </summary>
    /// <param name="message">The message to check.</param>
    /// <param name="guildID">The ID of the guild the message originates from.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A result that may or not have succeeded.</returns>
    public async Task<Result> HandleAsync(IMessage message, Optional<Snowflake> guildID, CancellationToken ct = default)
    {
        if (message.Author.IsBot.OrDefault(false) || !guildID.IsDefined(out var gid) || string.IsNullOrWhiteSpace(message.Content))
        {
            return Result.FromSuccess();
        }

        var configResult = await _mediator.Send(new GetGuild.PhishingConfigRequest(gid));

        if (!configResult.IsDefined(out var config) || !config.ScanLinks)
        {
            return (Result)configResult;
        }

        var phishingResult = await _phishing.DetectLinkPhishingAsync(message.Content);

        if (!phishingResult.IsDefined(out var detectionResult))
        {
            return Result.FromSuccess();
        }

        return await HandlePhishingAsync(config, message.Author, gid, $"{detectionResult}");
    }


    private async Task<Result> HandlePhishingAsync(GuildPhishingConfig config, IUser user, Snowflake guildID, string reason)
    {
        var actionTask = config.DetectionAction switch
        {
            InfractionType.Kick => _infractions.AddUserKickAsync(guildID, user, _self, reason),
            InfractionType.Ban  => _infractions.AddUserBanAsync(guildID, user, _self, reason),
            _                   => Task.FromResult(Result.FromSuccess())
        };

        return await actionTask;
    }
}
