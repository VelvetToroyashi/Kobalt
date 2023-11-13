using System.Security.Claims;

namespace Kobalt.Dashboard.Extensions;

public static class ClaimsExtensions
{
    public static bool IsAuthenticated(this ClaimsPrincipal principal)
    {
        return principal.Identity?.IsAuthenticated ?? false;
    }
}