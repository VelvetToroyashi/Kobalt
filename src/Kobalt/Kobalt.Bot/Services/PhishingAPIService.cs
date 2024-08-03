using System.Text.Json;
using System.Text.RegularExpressions;
using Humanizer;
using Kobalt.Phishing.Shared.Interfaces;
using Kobalt.Phishing.Shared.Models;
using Kobalt.Shared.Extensions;
using Microsoft.Extensions.Options;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Bot.Services;

/// <summary>
/// An API wrapper for the phishing API.
/// </summary>
public partial class PhishingAPIService(IKobaltRestPhishingAPI phishing)
{
    [GeneratedRegex(@"[.]*(?:https?:\/\/(www\.)?)?(?<link>[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6})\b([-a-zA-Z0-9()@:%_\+.~#?&\/\/=]*)")]
    private static partial Regex DomainRegex();

    /// <summary>
    /// Checks if a user is suspicious.
    /// </summary>
    /// <param name="guildID">The ID of the guild checking.</param>
    /// <param name="userID">The ID of the user to be checked.</param>
    /// <param name="username">The username of the user to check.</param>
    /// <param name="avatarHash">The hash of the user's avatar to check.</param>
    /// <returns>A result containing</returns>
    public async Task<UserPhishingDetectionResult> DetectUserPhishingAsync(Snowflake guildID, Snowflake userID, string username, string? avatarHash)
    {
        CheckUserRequest request = new(userID, username, avatarHash);
        var match = await phishing.CheckUserAsync(guildID, request);

        return match;
    }

    /// <summary>
    /// Detects potential phishing domains in any given message.
    /// </summary>
    /// <param name="content">The content in the message, to extract domains from.</param>
    /// <returns>
    /// If the conent contains a link, a successful result with the reason, otherwise
    /// <see cref="InvalidOperationError"/> if there were no domains at all, or
    /// <see cref="NotFoundError"/> if no domain matched.
    /// </returns>
    public async Task<Result<string>> DetectLinkPhishingAsync(string content)
    {
        var domains = DomainRegex().Matches(content).Select(d => d.Groups["link"].Value).ToArray();

        if (!domains.Any())
        {
            return new InvalidOperationError("No domains were found in the content.");
        }

        var matchResult = await ResultExtensions.TryCatchAsync(() => phishing.CheckLinksAsync(domains));

        if (!matchResult.IsDefined(out var match) || match.Match is false)
        {
            return new NotFoundError(matchResult.Error!.Message);
        }

        return Result<string>.FromSuccess(match!.DetectionReason!);

    }

    /// <summary>
    /// Creates a blacklisted avatar for a specific guild.
    /// </summary>
    /// <param name="guildID">The ID of the guild the avatar is being submitted in.</param>
    /// <param name="addedBy">The ID of the user this was submitted by.</param>
    /// <param name="url">The URL pointing to the content to be submitted.</param>
    /// <param name="category">The category that best represents the image; this must be a unique identifier.</param>
    /// <returns>A result that may or not have succeeded.</returns>
    public async Task<Result> CreateSuspiciousGuildAvatarAsync(Snowflake guildID, Snowflake addedBy, string url, string category)
    {
        var request = new SubmitAvatarRequest(url, category, addedBy);
        var result = await ResultExtensions.TryCatchAsync(() => phishing.CreateSuspiciousUserAvatarAsync(guildID, request));

        return result;
    }

    /// <summary>
    /// Creates a new username filter
    /// </summary>
    /// <param name="guildID">The ID of the guild to create a filter in.</param>
    /// <param name="usernamePattern">The pattern to match usernames against.</param>
    /// <param name="parseType">How to parse the username, either as regex or a string literal.</param>
    /// <returns>A result that may or not have succeded</returns>
    public async Task<Result> CreateSuspiciousGuildUsernameAsync(Snowflake guildID, string usernamePattern, UsernameParseType parseType)
    {
        if (parseType is UsernameParseType.Regex)
        {
            var valid = ResultExtensions.TryCatch((pattern) => Regex.Match("", pattern), usernamePattern);

            if (!valid.IsSuccess)
            {
                var exception = (RegexParseException)((ExceptionError)valid.Error).Exception;
                return new InvalidOperationError
                (
                    $"The regex pattern provided is invalid ({exception.Error.Humanize()} at character {exception.Offset}).\n" +
                    $"`{usernamePattern.Insert(exception.Offset, "!! ➜")}`"
                );
            }
        }

        var request = new SubmitUsernameRequest(usernamePattern, parseType);
        var result = await ResultExtensions.TryCatchAsync(() => phishing.CreateSuspiciousUsernameAsync(guildID, request));

        return result;
    }
}
