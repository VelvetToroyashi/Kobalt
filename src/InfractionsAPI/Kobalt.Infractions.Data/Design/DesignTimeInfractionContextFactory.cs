﻿using Kobalt.Shared.Extensions;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kobalt.Infractions.Data.Design;

public class DesignTimeInfractionContextFactory : IDesignTimeDbContextFactory<InfractionContext>
{
    public InfractionContext CreateDbContext(string[] args)
        =>
        new ServiceCollection()
        .AddLogging()
        .AddSingleton(new ConfigurationBuilder().AddUserSecrets<InfractionContext>().Build())
        .AddDbContextFactory<InfractionContext>("Infractions")
        .BuildServiceProvider()
        .GetRequiredService<InfractionContext>();
}