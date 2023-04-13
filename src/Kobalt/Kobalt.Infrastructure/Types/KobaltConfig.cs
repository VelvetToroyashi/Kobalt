namespace Kobalt.Infrastructure.Types;

public class KobaltConfig
{
    public required KobaltDiscordConfig Discord { get; set; }
};

public record KobaltDiscordConfig(string Token, int ShardCount, string? PublicKey);
