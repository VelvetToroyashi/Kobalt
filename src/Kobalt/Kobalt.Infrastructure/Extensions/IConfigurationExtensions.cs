using Kobalt.Infrastructure.Types;
using Microsoft.Extensions.Configuration;

namespace Kobalt.ReminderService.API.Extensions;

public static class IConfigurationExtensions
{
    public static KobaltConfig GetKobaltConfig(this IConfiguration configuration)
    {
        var config = configuration.Get<KobaltConfig>()!;
        return config;
    }
}
