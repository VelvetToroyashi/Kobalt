namespace Kobalt.Infrastructure.Types;

public class KobaltConfig
{
    public KobaltDiscordConfig Discord { get; set; }
};

public record KobaltDiscordConfig(string Token);
