using DotNet.Testcontainers.Builders;
using Kobalt.Infractions.Data.Entities;
using Kobalt.Infractions.Infrastructure.Mediator.Mediator;
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
    
    
    private InfractionContext _db;
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
        
        var db = new ServiceCollection().AddDbContext<InfractionContext>(o => o.UseNpgsql(_container.GetConnectionString()).UseSnakeCaseNamingConvention()).BuildServiceProvider();

        _db = db.GetRequiredService<InfractionContext>();
    }

    [SetUp]
    public async Task SetupAsync()
    {
        await _db.Database.EnsureCreatedAsync();
    }

    [TearDown]
    public async Task TeardownAsync()
    {
        await _db.Database.EnsureDeletedAsync();
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

        var infraction_1 = new InfractionCreatePayload(InfractionID, false, InfractionReason, UserID, ModeratorID, InfractionType.Ban, now, null);
        var infraction_2 = new InfractionCreatePayload(InfractionID + 1, false, InfractionReason, UserID + 1, ModeratorID, InfractionType.Ban, now, null);

        var handler = new BulkAddInfractionsForGuildHandler(_db);

        await handler.Handle(new(GuildID, new[] { infraction_1, infraction_2 }), CancellationToken.None);

        Infraction[] result = _db.Infractions.Local.ToArray();
        
        Assert.AreEqual(2, result.Length);
        
        Assert.AreEqual(infraction_1.Id, result[0].Id);
        Assert.AreEqual(infraction_1.UserID, result[0].UserID);
        Assert.AreEqual(infraction_1.ModeratorID, result[0].ModeratorID);
        
        Assert.AreEqual(infraction_2.Id, result[1].Id);
        Assert.AreEqual(infraction_2.UserID, result[1].UserID);
        Assert.AreEqual(infraction_2.ModeratorID, result[1].ModeratorID);
    }
}
