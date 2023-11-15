using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Caching.Abstractions;
using Remora.Discord.Caching.Services;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Dashboard.Services.Remora;

public class TokenScopedDiscordRestUserAPI(IDiscordRestUserAPI actual, CacheService cache, ITokenRepository tokens) : IDiscordRestUserAPI
{

    public Task<Result<IUser>> GetCurrentUserAsync(CancellationToken ct = default) => actual.GetCurrentUserAsync(ct);

    public Task<Result<IUser>> GetUserAsync(Snowflake userID, CancellationToken ct = default) => actual.GetUserAsync(userID, ct);

    public Task<Result<IUser>> ModifyCurrentUserAsync(Optional<string> username, Optional<Stream?> avatar = default, CancellationToken ct = default)
        => actual.ModifyCurrentUserAsync(username, avatar, ct);

    public async Task<Result<IReadOnlyList<IPartialGuild>>> GetCurrentUserGuildsAsync
    (
        Optional<Snowflake> before = default,
        Optional<Snowflake> after = default,
        Optional<int> limit = default,
        Optional<bool> withCounts = default,
        CancellationToken ct = default
    )
    {
        var token = await tokens.GetTokenAsync(ct);
        var cacheResult = await cache.TryGetValueAsync<IReadOnlyList<IPartialGuild>>(CacheKey.LocalizedStringKey(token, "user-guilds"), ct);
        
        if (cacheResult.IsSuccess)
        {
            return cacheResult;
        }
        
        var result = await actual.GetCurrentUserGuildsAsync(before, after, limit, withCounts, ct);
        if (!result.IsSuccess)
        {
            return result;
        }
        
        await cache.CacheAsync(CacheKey.LocalizedStringKey(token, "user-guilds"), result.Entity, ct);
        
        return result;
    }

    public Task<Result<IGuildMember>> GetCurrentUserGuildMemberAsync(Snowflake guildID, CancellationToken ct = default) => actual.GetCurrentUserGuildMemberAsync(guildID, ct);

    public Task<Result> LeaveGuildAsync(Snowflake guildID, CancellationToken ct = default) => actual.LeaveGuildAsync(guildID, ct);

    public Task<Result<IReadOnlyList<IChannel>>> GetUserDMsAsync(CancellationToken ct = default) => actual.GetUserDMsAsync(ct);

    public Task<Result<IChannel>> CreateDMAsync(Snowflake recipientID, CancellationToken ct = default) => actual.CreateDMAsync(recipientID, ct);

    public Task<Result<IReadOnlyList<IConnection>>> GetCurrentUserConnectionsAsync(CancellationToken ct = default) => actual.GetCurrentUserConnectionsAsync(ct);

    public Task<Result<IApplicationRoleConnection>> GetCurrentUserApplicationRoleConnectionAsync(Snowflake applicationID, CancellationToken ct = default) => actual.GetCurrentUserApplicationRoleConnectionAsync(applicationID, ct);

    public Task<Result<IApplicationRoleConnection>> UpdateCurrentUserApplicationRoleConnectionAsync
    (
        Snowflake applicationID,
        Optional<string> platformName = default,
        Optional<string> platformUsername = default,
        Optional<IReadOnlyDictionary<string, string>> metadata = default,
        CancellationToken ct = default
    ) => actual.UpdateCurrentUserApplicationRoleConnectionAsync(applicationID, platformName, platformUsername, metadata, ct);
}