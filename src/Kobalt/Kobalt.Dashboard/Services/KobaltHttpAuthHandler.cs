using System.Net.Http.Headers;

namespace Kobalt.Dashboard.Services;

public class KobaltHttpAuthHandler(ITokenRepository tokens) : DelegatingHandler
{
    protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await tokens.GetTokenAsync(cancellationToken));
        
        return await base.SendAsync(request, cancellationToken);
    }

}