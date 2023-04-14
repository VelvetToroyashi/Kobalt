namespace Kobalt.Infrastructure.Types;

public sealed class KobaltConfig
{
    public required int ApiPort { get; set; } = 3140;
    public required KobaltDiscordConfig Discord { get; set; }
};

public record KobaltDiscordConfig(string Token, int ShardCount, string? PublicKey);
