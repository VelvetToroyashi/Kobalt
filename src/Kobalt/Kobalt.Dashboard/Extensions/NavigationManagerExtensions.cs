using Microsoft.AspNetCore.Components;

namespace Kobalt.Dashboard.Extensions;

public static class NavigationManagerExtensions
{
    public static string GetDiscordLoginUrl(this NavigationManager navigationManager, string? returnUrl = null)
    {
        var escapedReturnUrl = Uri.EscapeDataString('/' + (returnUrl ?? navigationManager.ToBaseRelativePath(navigationManager.Uri)));
        
        return $"{navigationManager.BaseUri}api/auth/login?returnUrl={escapedReturnUrl}";
    }
}