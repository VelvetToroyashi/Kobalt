using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CoenM.ImageHash;
using CoenM.ImageHash.HashAlgorithms;
using Kobalt.Bot.Data.Entities.Phishing;
using Kobalt.Bot.Data.MediatR.Phishing;
using Kobalt.Shared.Extensions;
using Kobalt.Shared.Models.Phishing;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Remora.Rest.Core;
using Remora.Results;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Kobalt.Bot.Services;

using UsernameDetectionResult = (bool Matched, string? Username, bool Global);


/// <summary>
/// A service that aggregates various sources for phishing data.
/// </summary>
public class PhishingService : BackgroundService
{
    private const string DiscordCDNAvatars = "https://cdn.discordapp.com/avatars/{0}/{1}.png?size=256";
    private const string DiscordBadLinks = "https://cdn.discordapp.com/bad-domains/updated_hashes.json";
    private const string FishFish = "https://api.fishfish.gg/v1/domains";

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
    public async Task<UserPhishingDetectionResult> CheckUserAsync(Snowflake guildID, CheckUserRequest request)
    {
        var usernames = await _mediator.Send(new GetSuspiciousUsernames.Request(guildID.Value));

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
    public Optional<string>  CheckLinks(IReadOnlyList<string> domains)
    {
        HashSet<byte[]>? domainList = _cache.Get<HashSet<byte[]>>("phishing-domains");

        domainList.Add(SHA256.HashData("wahs.uk"u8.ToArray()));

        if (domainList is null)
        {
            //TODO: Throw so we can return a 500
            return default;
        }

        Regex regex = PhishingDetectionService.DomainRegex();

        foreach (var domain in domains)
        {
            Match domainMatch = regex.Match(domain);

            if (!domainMatch.Success)
            {
                continue;
            }

            string domainString = domainMatch.Groups["link"].Value;
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(domainString));

            foreach (var domainHash in domainList)
            {
                if (domainHash.SequenceEqual(hash))
                {
                    return $"ANTI-PHISHING: Link matched `{domainString}`";
                }
            }
        }

        return default;
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

    private async Task<Result<(SuspiciousAvatar Avatar, int Score)>> CheckAvatarAsync(Snowflake guildID, Snowflake userID, string? userAvatarHash)
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

        var avatarHashes = await _mediator.Send(new GetSuspiciousAvatars.Request(guildID.Value));

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

        var firstTenFishDomains = fishFishResult.Entity.Take(10);

        Console.WriteLine($"Domain format: {string.Join(',', firstTenFishDomains)}.");

        var domains = new HashSet<byte[]>(discordResult.Entity!.Count + fishFishResult.Entity!.Count, new ByteEqualityComparer());

        foreach(var domain in discordResult.Entity)
        {
            domains.Add(Convert.FromHexString(domain));
        }

        foreach (var domain in fishFishResult.Entity!)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(domain));

            domains.Add(hash);
        }

        _cache.Set("phishing-domains", domains, TimeSpan.FromDays(1));

        return Result.FromSuccess();
    }

}

file class ByteEqualityComparer : IEqualityComparer<byte[]>
{
    public bool Equals(byte[]? a, byte[]? b)
    {
        if (a == b) return true;
        if (a is null || b is null) return false;
        return a.AsSpan().SequenceEqual(b.AsSpan());
    }

    public int GetHashCode(byte[] array)
    {
        HashCode hashCode = default;
        hashCode.AddBytes(array);
        return hashCode.ToHashCode();
    }
}
