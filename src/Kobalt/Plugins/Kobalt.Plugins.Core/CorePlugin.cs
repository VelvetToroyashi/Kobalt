﻿using System.Reflection;
using Kobalt.Data;
using Kobalt.Infrastructure.Extensions;
using Kobalt.Infrastructure.Services.Booru;
using Kobalt.Plugins.Core;
using Kobalt.Plugins.Core.Services;
using Kobalt.Shared.Extensions;
using Kobalt.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Remora.Discord.Extensions.Extensions;
using Remora.Plugins.Abstractions;
using Remora.Plugins.Abstractions.Attributes;

[assembly: RemoraPlugin(typeof(CorePlugin))]

namespace Kobalt.Plugins.Core;

public class CorePlugin : PluginDescriptor, IMigratablePlugin
{
    public override string Name => "Kobalt Core";
    public override string Description => "The core functionality of Kobalt.";

    public override Result ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<IChannelLoggerService, ChannelLoggerService>();
        services.AddTransient<ImageOverlayService>();
        services.AddOffsetServices();
        services.AddHttpClient("booru");
        services.AddTransient<BooruSearchService>();

        services.AddMediatR(s => s.RegisterServicesFromAssemblyContaining<KobaltContext>()
                                          .RegisterServicesFromAssemblyContaining<CorePlugin>());

        services.AddDbContextFactory<KobaltContext>("Kobalt");

        services.AddCommandGroupsFromAssembly(Assembly.GetExecutingAssembly(), typeFilter: t => !t.IsNested);
        services.AddInteractivityFromAssembly(Assembly.GetExecutingAssembly());

        return Result.FromSuccess();
    }

    public Task<Result> MigrateAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
    {
        var db = serviceProvider.GetRequiredService<KobaltContext>();

        return ResultExtensions.TryCatchAsync(() => db.Database.MigrateAsync(ct));
    }
}
