using System.Runtime.CompilerServices;
using Kobalt.Infrastructure.Extensions;
using Kobalt.Infrastructure.Services.Booru;
using Kobalt.Plugins.Core;
using Kobalt.Plugins.Core.Data;
using Kobalt.Plugins.Core.Services;
using Kobalt.Shared.Extensions;
using Kobalt.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Remora.Discord.Extensions.Extensions;
using Remora.Plugins.Abstractions;
using Remora.Plugins.Abstractions.Attributes;

[assembly: RemoraPlugin(typeof(CorePlugin))]
[assembly: InternalsVisibleTo("Kobalt.Plugins.Core.Tests")]

namespace Kobalt.Plugins.Core;

public class CorePlugin : PluginDescriptor, IMigratablePlugin
{
    public override string Name => "Kobalt Core";
    public override string Description => "The core functionality of Kobalt.";

    public override Result ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<IChannelLoggerService, ChannelLoggerService>();
        services.AddTransient<ImageOverlayService>();

        services.AddHttpClient("booru");
        services.AddTransient<BooruSearchService>();

        services.AddMediatR(s => s.RegisterServicesFromAssemblyContaining<KobaltContext>()
                                          .RegisterServicesFromAssemblyContaining<CorePlugin>());

        services.AddDbContextFactory<KobaltContext>("Kobalt");

        var asm = typeof(CorePlugin).Assembly;

        services.AddRespondersFromAssembly(asm);
        services.AddInteractivityFromAssembly(asm);
        services.AddCommandGroupsFromAssembly(asm, typeFilter: t => !t.IsNested);

        services.AddSingleton<AntiRaidV2Service>();
        services.AddSingleton<ChannelWatcherService>();

        return Result.FromSuccess();
    }

    public Task<Result> MigrateAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
    {
        var db = serviceProvider.GetRequiredService<IDbContextFactory<KobaltContext>>().CreateDbContext();

        return ResultExtensions.TryCatchAsync(() => db.Database.MigrateAsync(ct));
    }
}
