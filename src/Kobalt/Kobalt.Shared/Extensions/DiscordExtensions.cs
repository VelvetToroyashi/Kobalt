using Remora.Discord.API.Abstractions.Objects;

namespace Kobalt.Shared.Extensions;

public static class DiscordExtensions
{
    /// <summary>
    /// Gets the user's Discord tag.
    /// </summary>
    /// <param name="user">The user to get the tag for.</param>
    /// <returns>A formated string representing the user's tag, e.g. Nelly#0001</returns>
    public static string DiscordTag(this IUser user) => $"{user.Username}#{user.Discriminator:0000}";

    /// <summary>
    /// Turns a <see cref="DateTimeOffset"/> into a formatted timestamp.
    /// </summary>
    /// <param name="time">The time to convert.</param>
    /// <param name="format">The format to use.</param>
    /// <returns>The formatted timestamp.</returns>
    public static string ToTimestamp(this DateTimeOffset time, TimestampFormat format = TimestampFormat.RelativeTime) => $"<t:{time.ToUniversalTime().ToUnixTimeSeconds()}:{(char)format}>";
}

/// <summary>
/// Represents a format for a timestamp.
/// </summary>
public enum TimestampFormat : byte
{
    ShortTime = (byte)'t',
    LongTime = (byte)'T',
    ShortDate = (byte)'d',
    LongDate = (byte)'D',
    ShortDateTime = (byte)'f',
    LongDateTime = (byte)'F',
    RelativeTime = (byte)'R',
}
