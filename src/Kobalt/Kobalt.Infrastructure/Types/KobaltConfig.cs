namespace Kobalt.Infrastructure.Types;

public sealed class KobaltConfig
{
    public required KobaltBotConfig Bot { get; set; } = new();
    public required KobaltDiscordConfig Discord { get; set; }
};

/// <summary>
/// Represents the configuration for Discord.
/// </summary>
/// <param name="Token">The bot's token.</param>
/// <param name="ShardCount">The amount of shards the bot will run.</param>
/// <param name="PublicKey">The bot's public key for verifying interactions.</param>
public record KobaltDiscordConfig(string Token, int ShardCount, string? PublicKey);

/// <summary>
/// Represents the configuration for the bot.
/// </summary>
/// <param name="RemindersUrl">The API URL for reminders.</param>
/// <param name="PhishingUrl">The API URL for phishing.</param>
/// <param name="InfractionsUrl">The API URL for infractions.</param>
/// <param name="EnableReminders">Whether reminders are enabled.</param>
/// <param name="EnablePhishing">Whether phishing is enabled.</param>
/// <param name="EnableInfractions">Whether infractions are enabled.</param>
/// <param name="EnableHTTPInteractions">Whether HTTP interactions are enabled.</param>
public record KobaltBotConfig
(
    string? RemindersUrl = null,
    string? PhishingUrl = null,
    string? InfractionsUrl = null,
    bool EnableReminders = true,
    bool EnablePhishing = true,
    bool EnableInfractions = true,
    bool EnableHTTPInteractions = true
);