using System.Text.RegularExpressions;
using Kobalt.Bot.Data.Entities;
using Kobalt.Bot.Data.MediatR.Guilds;
using Kobalt.Infractions.Shared;
using Kobalt.Shared.Models.Phishing;
using MediatR;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;

namespace Kobalt.Bot.Services;

/// <summary>
/// Service for detecting phishing attempts.
/// </summary>
public partial class PhishingDetectionService
{
    [GeneratedRegex(@"[.]*(?:https?:\/\/(www\.)?)?(?<link>[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6})\b([-a-zA-Z0-9()@:%_\+.~#?&\/\/=]*)")]
    public static partial Regex DomainRegex();

    private readonly IUser _self;
    private readonly IMediator _mediator;
    private readonly PhishingService _phishing;
    private readonly InfractionAPIService _infractions;

    public PhishingDetectionService(IUser self, IMediator mediator, PhishingService phishing, InfractionAPIService infractions)
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
        var configResult = await _mediator.Send(new GetGuild.PhishingConfigRequest(guildID), ct);

        if (!configResult.IsDefined(out var config) || !config.ScanUsers)
        {
            return (Result)configResult;
        }

        var phishingRequest = new CheckUserRequest(user.ID, user.Username, user.Avatar?.Value);
        var phishingResult = await _phishing.CheckUserAsync(guildID, phishingRequest);

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

        var diff = DateTimeOffset.UtcNow - message.ID.Timestamp;

        Console.WriteLine($"Time to detection: {diff.TotalMilliseconds}ms");
        var configResult = await _mediator.Send(new GetGuild.PhishingConfigRequest(gid));

        if (!configResult.IsDefined(out var config) || !config.ScanLinks)
        {
            return (Result)configResult;
        }

        var links = DomainRegex().Matches(message.Content).Select(d => d.Groups["link"].Value).ToArray();


        var phishingResult = _phishing.CheckLinks(links);

        if (!phishingResult.IsDefined(out var link))
        {
            return Result.FromSuccess();
        }

        return await HandlePhishingAsync(config, message.Author, gid, link);
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
