using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CoenM.ImageHash;
using CoenM.ImageHash.HashAlgorithms;
using Kobalt.Phishing.Data.Entities;
using Kobalt.Phishing.Data.MediatR;
using Kobalt.Phishing.Shared.Models;
using Kobalt.Shared.Extensions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Remora.Rest.Core;
using Remora.Results;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Kobalt.Phishing.API.Services;

// TODO: Using alias when .NET 8 is released.
internal record UsernameDetectionResult(bool Matched, string? Username, bool Global);

/// <summary>
/// A service that aggregates various sources for phishing data.
/// </summary>
public class PhishingService : BackgroundService
{
    private const string DiscordCDNAvatars = "https://cdn.discordapp.com/avatars/{0}/{1}.png?size=256";
    private const string DiscordBadLinks = "https://cdn.discordapp.com/bad-domains/updated_hashes.json";
    private const string FishFish = "https://phish.sinking.yachts/v2/all"; // TODO: Replace with fishfish.gg when the API is stabilized.

    private readonly HttpClient _client;
    private readonly IMediator _mediator;
    private readonly IMemoryCache _cache;

    public PhishingService(IHttpClientFactory client, IMediator mediator, IMemoryCache cache)
    {
        _client = client.CreateClient("Phishing");
        _mediator = mediator;
        _cache = cache;
    }

    /// <summary>
    /// Checks a user's profile (avatar, username) for known phishing data.
    /// </summary>
    /// <param name="guildID">The ID of the guild to evaluate for.</param>
    /// <param name="request">The data pertaining to the user.</param>
    /// <returns>A result indicating whether there was a match.</returns>
    public async Task<UserPhishingDetectionResult> CheckUserAsync(ulong guildID, CheckUserRequest request)
    {
        var usernames = await _mediator.Send(new GetSuspiciousUsernames.Request(guildID));

        var matchResult = CheckUsername(request.Username, usernames);

        if (matchResult.Matched)
        {
            // TODO: Fuzzy match literal names, and allow guilds to specify a threshold
            return new UserPhishingDetectionResult(100, true, matchResult.Global, "ANTI-PHISHING: Username matched a preset filter.");
        }

        var avatarResult = await CheckAvatarAsync(guildID, request.UserID, request.AvatarHash);

        if (avatarResult.IsDefined(out var result))
        {
            return new UserPhishingDetectionResult
            (
                result.Score,
                true,
                result.Avatar.GuildID is null,
                $"ANTI-PHISHING: Avatar matched `{result.Avatar.Category}` ({result.Score})."
            );
        }

        return new UserPhishingDetectionResult(null, false, false, null);
    }

    /// <summary>
    /// Checks multiple domains for phishing.
    /// </summary>
    /// <param name="domains">The domains to check.</param>
    /// <returns>A result for whether or not any of the domains matched.</returns>
    public UserPhishingDetectionResult CheckLinksAsync(IReadOnlyList<string> domains)
    {
        var domainList = _cache.Get<HashSet<byte[]>>("phishing-domains");

        if (domainList is null)
        {
            //TODO: Throw so we can return a 500
            return new UserPhishingDetectionResult(null, false, false, null);
        }

        var matches = domains.Where(x => domainList.Contains(SHA256.HashData(Encoding.UTF8.GetBytes(x))));

        if (matches.FirstOrDefault() is {} match)
        {
            return new UserPhishingDetectionResult(null, true, true, $"ANTI-PHISHING: Link matched `{match}`.");
        }

        return new UserPhishingDetectionResult(null, false, false, null);
    }

    public async Task<Result<byte[]>> HashImageAsync(string url)
    {
        var bytes = await ResultExtensions.TryCatchAsync(() => _client.GetByteArrayAsync(url));

        if (!bytes.IsDefined(out var result))
        {
            return Result<byte[]>.FromError(bytes.Error!);
        }

        using var image = Image.Load<Rgba32>(result);
        image.Mutate(img => img.Resize(new Size(256, 256)));

        var hasher = new PerceptualHash();
        var hashResult = ResultExtensions.TryCatch((img) => BitConverter.GetBytes(hasher.Hash(img)), image);

        return hashResult;
    }

    private async Task<Result<(SuspiciousAvatar Avatar, int Score)>> CheckAvatarAsync(ulong guildID, Snowflake userID, string? userAvatarHash)
    {
        if (userAvatarHash is null)
        {
            return new InvalidOperationError("User has no avatar.");
        }

        var avatarBytesResult = await ResultExtensions.TryCatchAsync
        (
            () => _client.GetByteArrayAsync(string.Format(DiscordCDNAvatars, userID, userAvatarHash))
        );

        if (!avatarBytesResult.IsDefined(out var avatarBytes))
        {
            return Result<(SuspiciousAvatar, int)>.FromError(avatarBytesResult.Error!);
        }

        var hasher = new PerceptualHash();
        var hash = hasher.Hash(Image.LoadPixelData<Rgba32>(avatarBytes, 256, 256));

        var avatarHashes = await _mediator.Send(new GetSuspiciousAvatars.Request(guildID));

        var match = avatarHashes.FirstOrDefault(x => CompareHash.Similarity(hash, BitConverter.ToUInt64(x.Phash)) > 0.94);

        if (match is not null)
        {
            return (match, (int)(CompareHash.Similarity(hash, BitConverter.ToUInt64(match.Phash)) * 100));
        }

        return new NotFoundError("No matching avatar found.");
    }

    private UsernameDetectionResult CheckUsername(string requestUsername, IEnumerable<SuspiciousUsername> usernames)
    {
        var literals = usernames.Where(x => x.ParseType == UsernameParseType.Literal);
        var regexes = usernames.Where(x => x.ParseType == UsernameParseType.Regex);

        var literalMatch = literals.FirstOrDefault(x => x.UsernamePattern.Equals(requestUsername, StringComparison.OrdinalIgnoreCase));

        if (literalMatch is not null)
        {
            return new UsernameDetectionResult(true, literalMatch.UsernamePattern, literalMatch.GuildID is null);
        }

        var regexMatch = regexes.FirstOrDefault
        (
            x => ResultExtensions.TryCatch
            (
                static ((string username, string pattern) state) => Regex.IsMatch(state.pattern, state.username),
                (x.UsernamePattern, requestUsername)
            )
            .IsDefined(out var res) && res
        );

        if (regexMatch is not null)
        {
            return new UsernameDetectionResult(true, regexMatch.UsernamePattern, regexMatch.GuildID is null);
        }

        return new UsernameDetectionResult(false, null, false);
    }



    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var domainResult = await GetDomainsAsync();

            if (domainResult.IsSuccess)
            {
                await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
            }
            else
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    /// <summary>
    /// Gets all phishing domains from a variety of sources.
    /// </summary>
    /// <returns>A result that may or not have succeeded.</returns>
    internal async Task<Result> GetDomainsAsync()
    {
        var discordResult = await ResultExtensions.TryCatchAsync(() => _client.GetFromJsonAsync<IReadOnlyList<string>>(DiscordBadLinks));

        if (!discordResult.IsSuccess)
        {
            return (Result)discordResult;
        }

        var fishFishResult = await ResultExtensions.TryCatchAsync(() => _client.GetFromJsonAsync<IReadOnlyList<string>>(FishFish));

        if (!fishFishResult.IsSuccess)
        {
            return (Result)fishFishResult;
        }

        var domains = new HashSet<byte[]>(discordResult.Entity!.Count + fishFishResult.Entity!.Count);

        foreach (var domain in discordResult.Entity!)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(domain));

            domains.Add(hash);
        }

        _cache.Set("phishing-domains", domains, TimeSpan.FromDays(1));

        return Result.FromSuccess();
    }

}
