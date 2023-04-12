using DotNet.Testcontainers.Builders;
using Kobalt.Infractions.Data.Entities;
using Kobalt.Infractions.Infrastructure.Mediator.Mediator;
using Kobalt.Infractions.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Kobalt.Infractions.Data.Tests;

public class GetInfractionsForUserTests
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
        _db.ChangeTracker.Clear();
        await _db.Database.EnsureDeletedAsync();
    }

    [OneTimeTearDown]
    public async Task TeardownGlobalAsync()
    {
        await _container.DisposeAsync();
    }
    
    [Test]
    public async Task GetsInfractionsForUserWhenExists()
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

        _db.Infractions.Add(inf);
        await _db.SaveChangesAsync();
        
        var handler = new GetInfractionsForUserHandler(_db);

        var res = await handler.Handle(new(GuildID, UserID), default);

        Assert.IsNotEmpty(res);
        Assert.AreEqual(InfractionID, res.First().Id);
    }
    
    [Test]
    public async Task ReturnsErrorForNonexistentGuild()
    {
        var handler = new GetInfractionsForUserHandler(_db);
        
        var res = await handler.Handle(new(GuildID, UserID), default);

        Assert.IsEmpty(res);
    }
}
