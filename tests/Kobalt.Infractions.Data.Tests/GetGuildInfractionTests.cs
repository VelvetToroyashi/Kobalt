using DotNet.Testcontainers.Builders;
using Kobalt.Infractions.Data.Entities;
using Kobalt.Infractions.Data.Mediator;
using Kobalt.Infractions.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Remora.Results;
using Testcontainers.PostgreSql;

namespace Kobalt.Infractions.Data.Tests;

public class GetGuildInfractionTests
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
        var db = _db.CreateDbContext();
        
        db.ChangeTracker.Clear();
        await db.Database.EnsureDeletedAsync();
    }

    [OneTimeTearDown]
    public async Task TeardownGlobalAsync()
    {
        await _container.DisposeAsync();
    }

    [Test]
    public async Task ReturnsInfractionForCorrectGuildID()
    {
        var inf = new Infraction
        {
            Reason = InfractionReason,
            UserID = UserID,
            GuildID = GuildID,
            ModeratorID = ModeratorID,
            IsProcessable = true,
            Type = InfractionType.Ban,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow
        };

        var context = await _db.CreateDbContextAsync();
        
        context.Infractions.Add(inf);
        await context.SaveChangesAsync();
        
        var handler = new GetGuildInfractionHandler(_db);

        var res = await handler.Handle(new(InfractionID, GuildID), default);
        
        Assert.Multiple(() =>
        {
            Assert.That(res.IsSuccess);
            Assert.That(res.Entity.Id, Is.EqualTo(InfractionID));
            Assert.That(res.Entity.Type, Is.EqualTo(inf.Type));
        });
    }

    [Test]
    public async Task ReturnsErrorForNonexistentInfraction()
    {
        var handler = new GetGuildInfractionHandler(_db);

        var res = await handler.Handle(new(InfractionID, GuildID), default);

        Assert.IsFalse(res.IsSuccess);
        Assert.IsInstanceOf<NotFoundError>(res.Error);
    }

    [Test]
    public async Task ReturnsErrorForIncorrectGuildID()
    {
        var inf = new Infraction
        {
            Reason = InfractionReason,
            UserID = UserID,
            GuildID = GuildID,
            ModeratorID = ModeratorID,
            IsProcessable = true,
            Type = InfractionType.Ban,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow
        };
        
        var context = await _db.CreateDbContextAsync();

        context.Infractions.Add(inf);
        await context.SaveChangesAsync();
    
        var handler = new GetGuildInfractionHandler(_db);

        var res = await handler.Handle(new(InfractionID, GuildID + 1), default);
        
        Assert.IsFalse(res.IsSuccess);
        Assert.IsInstanceOf<NotFoundError>(res.Error);
    }
}
