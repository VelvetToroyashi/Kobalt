using System.Net.Http.Headers;
using Kobalt.Dashboard.Extensions;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Caching.Abstractions.Services;
using Remora.Discord.Rest.Extensions;
using Remora.Rest;

namespace Kobalt.Dashboard.Services;

public class DashboardRestClient
{
    private readonly ICacheProvider _cache;
    private readonly IRestHttpClient _rest;
    private readonly ITokenRepository _tokens;
    
    public DashboardRestClient(ICacheProvider cache, IRestHttpClient restClient, ITokenRepository tokenRepository, IHttpContextAccessor httpContext)
    {
        _cache = cache;
        _rest = restClient;
        _tokens = tokenRepository;

        var userId = httpContext.HttpContext?.User.GetUserID() ?? throw new InvalidOperationException("No authenticated user found.");
        _rest.WithCustomization
        (
            b => b.WithRateLimitContext(_cache)
            .With(message => message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens.GetToken(userId)!.AccessToken))
            .SkipAuthorization()
        );
    }
}