using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Humanizer;
using Kobalt.Phishing.Shared.Models;
using Kobalt.Shared.Extensions;
using Microsoft.Extensions.Options;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Plugins.Core.Services;

/// <summary>
/// An API wrapper for the phishing API.
/// </summary>
public partial class PhishingAPIService
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    [GeneratedRegex(@"[.]*(?:https?:\/\/(www\.)?)?(?<link>[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6})\b([-a-zA-Z0-9()@:%_\+.~#?&\/\/=]*)")]
    private static partial Regex DomainRegex();

    public PhishingAPIService(IHttpClientFactory client, IOptionsMonitor<JsonSerializerOptions> jsonOptions)
    {
        _client = client.CreateClient("Phishing");
        _jsonOptions = jsonOptions.Get("Discord");
    }

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
        using var response = await _client.PostAsJsonAsync($"/phishing/{guildID}/user", new CheckUserRequest(userID, username, avatarHash), _jsonOptions);
        var match = await response.Content.ReadFromJsonAsync<UserPhishingDetectionResult>(_jsonOptions);

        return match!;
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
        var domains = DomainRegex().Matches(content).ToArray();

        if (!domains.Any())
        {
            return new InvalidOperationError("No domains were found in the content.");
        }

        using var response = await _client.PostAsJsonAsync("/phishing/check/domains", domains.Select(d => d.Groups["link"].Value).ToArray(),  _jsonOptions);
        var match = await response.Content.ReadFromJsonAsync<UserPhishingDetectionResult>(_jsonOptions);

        return match!.Match ? Result<string>.FromSuccess(match!.DetectionReason!) : new NotFoundError("No matches were found.");

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
        using var response = await _client.PutAsJsonAsync($"/phishing/{guildID}/avatar", new SubmitAvatarRequest(url, category, addedBy), _jsonOptions);
        return response.IsSuccessStatusCode ? Result.FromSuccess() : new InvalidOperationError(await response.Content.ReadAsStringAsync());
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

        using var response = await _client.PutAsJsonAsync($"/phishing/{guildID}/username", new SubmitUsernameRequest(usernamePattern, parseType), _jsonOptions);
        return response.IsSuccessStatusCode ? Result.FromSuccess() : new InvalidOperationError(await response.Content.ReadAsStringAsync());
    }

}
