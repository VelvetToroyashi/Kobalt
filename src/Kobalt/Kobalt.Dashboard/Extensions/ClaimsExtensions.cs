using System.Security.Claims;

namespace Kobalt.Dashboard.Extensions;

public static class ClaimsExtensions
{
    public static bool IsAuthenticated(this ClaimsPrincipal principal)
    {
        return principal.Identity?.IsAuthenticated ?? false;
    }
    
    public static ulong GetUserID(this ClaimsPrincipal principal)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("No user is authenticated.");
        
        
        return ulong.Parse(id);
    }
    
    public static string GetUsername(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Name) ?? throw new InvalidOperationException("No user is authenticated.");
    }
}