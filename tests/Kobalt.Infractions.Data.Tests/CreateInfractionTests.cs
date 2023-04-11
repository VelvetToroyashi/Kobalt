using DotNet.Testcontainers.Builders;
using Kobalt.Infractions.Infrastructure.Mediator;
using Kobalt.Infractions.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Kobalt.Infractions.Data.Tests;

public class CreateInfractionTests
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
        _db.ChangeTracker.Clear();
    }

    [OneTimeTearDown]
    public async Task TeardownGlobalAsync()
    {
        await _container.DisposeAsync();
    }

    [Test]
    public async Task CreateInsertsValidInfractionCorrectly()
    {
        var req = new CreateInfractionRequest(InfractionReason, UserID, GuildID, ModeratorID, InfractionType.Ban, DateTimeOffset.UtcNow, null);

        var handler = new CreateInfractionHandler(_db);

        var res = await handler.Handle(req, default);
        
        Assert.AreEqual(UserID, res.UserID);
        Assert.AreEqual(GuildID, res.GuildID);
        Assert.AreEqual(InfractionType.Ban, res.Type);
        Assert.AreEqual(1, _db.Infractions.Local.Count);
    }

    [Test]
    public async Task CreateWithTemporaryInfractionIsProcessable()
    {
        var req = new CreateInfractionRequest(InfractionReason, UserID, GuildID, ModeratorID, InfractionType.Ban, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        var handler = new CreateInfractionHandler(_db);

        var res = await handler.Handle(req, default);
        
        Assert.AreEqual(true, _db.Infractions.Local.First().IsProcessable);
    }
}
