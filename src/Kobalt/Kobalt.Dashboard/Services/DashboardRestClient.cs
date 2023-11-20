using System.Net.Http.Headers;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Caching.Abstractions.Services;
using Remora.Discord.Rest;
using Remora.Discord.Rest.Extensions;
using Remora.Rest;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Dashboard.Services;

public class DashboardRestClient(ITokenRepository tokens, IRestHttpClient rest, ICacheProvider cache, IAsyncTokenStore tokenStore, IDiscordRestGuildAPI guilds, IDiscordRestUserAPI users)
{
    public async Task<Result<IReadOnlyList<IPartialGuild>>> GetCurrentUserGuildsAsync(CancellationToken ct = default)
    {
        // Safe: Calls "GetCurrentToken" internally.
        var token = await tokens.GetTokenAsync(ct);
        var requestResult = await rest.GetAsync<IReadOnlyList<IPartialGuild>>
        (
            "users/@me/guilds",
            b => b.WithRateLimitContext(cache)
            .SkipAuthorization()
            .With(d => d.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token)),
            ct: ct
        );
        
        return requestResult;
    }

    public Task<Result<IGuild>> GetGuildAsync(Snowflake guildID, CancellationToken ct = default)
        => guilds.GetGuildAsync(guildID, true, ct);

    public async Task<Result<bool>> IsSelfInGuildAsync(Snowflake guildID, CancellationToken ct = default)
    {
        var getGuildResult = await GetSelfMemberAsync(guildID, ct);
        return getGuildResult.MapOr(_ => true, false);
    }
    
    public Task<Result<IReadOnlyList<IChannel>>> GetGuildChannelsAsync(Snowflake guildID, CancellationToken ct = default)
        => guilds.GetGuildChannelsAsync(guildID, ct: ct);
    
    public Task<Result<IReadOnlyList<IRole>>> GetGuildRolesAsync(Snowflake guildID, CancellationToken ct = default)
        => guilds.GetGuildRolesAsync(guildID, ct: ct);
    
    public async Task<Result<IGuildMember>> GetSelfMemberAsync(Snowflake guildID, CancellationToken ct = default)
        => await guilds.GetGuildMemberAsync(guildID, new Snowflake(ulong.Parse(Convert.FromBase64String((await tokenStore.GetTokenAsync(ct)).Split('.')[0] + "=="))), ct: ct);

    public Task<Result<IUser>> ResolveUserAsync(ulong userId, CancellationToken ct = default)
        => users.GetUserAsync(new Snowflake(userId), ct);
}