using DotNet.Testcontainers.Builders;
using Kobalt.Infractions.Data.Entities;
using Kobalt.Infractions.Data.MediatR;
using Kobalt.Infractions.Shared;
using Kobalt.Infractions.Shared.Payloads;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Kobalt.Infractions.Data.Tests;

public class BulkAddInfractionsForGuildRequest 
{
    private const int InfractionID = 1;
    private const ulong GuildID = 123;
    private const ulong UserID = 456;
    private const ulong ModeratorID = 789;
    private const string InfractionReason = "Test infraction";
    
    
    private IDbContextFactory<InfractionContext> _db;
    // Ensure 'Expose daemon on tcp://localhost:2375 without TLS' is enabled if you're running under WSL2
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
                                                  .WithDockerEndpoint("tcp://localhost:2375")
                                                  .WithAutoRemove(true)
                                                  .WithUsername("kobalt")
                                                  .WithPassword("kobalt")
                                                  .WithDatabase("kobalt")
                                                  .WithPortBinding(5432, true)
                                                  .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
                                                  .Build();

    [OneTimeSetUp]
    public async Task Setup()
    {
        await _container.StartAsync();
        
        var db = new ServiceCollection().AddDbContextFactory<InfractionContext>(o => o.UseNpgsql(_container.GetConnectionString()).UseSnakeCaseNamingConvention()).BuildServiceProvider();

        _db = db.GetRequiredService<IDbContextFactory<InfractionContext>>();
    }

    [SetUp]
    public async Task SetupAsync()
    {
        await _db.CreateDbContext().Database.EnsureCreatedAsync();
    }

    [TearDown]
    public async Task TeardownAsync()
    {
        await _db.CreateDbContext().Database.EnsureDeletedAsync();
    }

    [OneTimeTearDown]
    public async Task TeardownGlobalAsync()
    {
        await _container.DisposeAsync();
    }

    [Test]
    public async Task InsertsBulkInfractionsCorrectly()
    {
        var now = DateTimeOffset.UtcNow;

        var infraction_1 = new InfractionCreatePayload(InfractionReason, UserID, ModeratorID, null, InfractionType.Ban, null);
        var infraction_2 = new InfractionCreatePayload(InfractionReason, UserID + 1, ModeratorID, null, InfractionType.Ban, null);

        var handler = new BulkAddInfractionsForGuildHandler(_db);

        await handler.Handle(new(GuildID, new[] { infraction_1, infraction_2 }), CancellationToken.None);

        Infraction[] result = _db.CreateDbContext().Infractions.Local.ToArray();
        
        Assert.That(result, Has.Length.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(result[0].Id, Is.EqualTo(1));
            Assert.That(result[0].UserID, Is.EqualTo(infraction_1.UserID));
            Assert.That(result[0].ModeratorID, Is.EqualTo(infraction_1.ModeratorID));

            Assert.That(result[1].Id, Is.EqualTo(2));
            Assert.That(result[1].UserID, Is.EqualTo(infraction_2.UserID));
            Assert.That(result[1].ModeratorID, Is.EqualTo(infraction_2.ModeratorID));
        });
    }
}
