using Kobalt.Shared.Extensions;
using Remora.Discord.API.Abstractions.Objects;

namespace Kobalt.Dashboard.Extensions;

public static class IUserExtensions
{
    public static string GetFormattedUsername(this IUser user)
        => user.Discriminator is 0 ? $"@{user.Username}" : user.DiscordTag();
}