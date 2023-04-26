using System.Net.WebSockets;
using Kobalt.Infractions.API.Services;
using Kobalt.Infractions.Data.Mediator;
using Kobalt.Infractions.Infrastructure.Interfaces;
using Kobalt.Infractions.Infrastructure.Mediator;
using Kobalt.Infractions.Infrastructure.Mediator.DTOs;
using Kobalt.Infractions.Shared;
using MassTransit;
using MassTransit.Testing;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Remora.Results;

namespace Kobalt.Infractions.API.Tests;

public class InfractionServiceTests
{
    private const int InfractionID = 1;
    private const ulong GuildID = 123;
    private const ulong UserID = 456;
    private const ulong ModeratorID = 789;
    private const string InfractionReason = "Test infraction";

    /// <summary>
    /// Tests that the infraction service can successfully make a call to the mediator and create an infraction.
    /// </summary>
    [Test]
    public async Task ServiceSuccessfullyCreatesInfraction()
    {
        var mediator = new Mock<IMediator>();
        var services = new ServiceCollection()
                      .AddMassTransitInMemoryTestHarness()
                      .BuildServiceProvider();

        var harness = services.GetRequiredService<InMemoryTestHarness>();
        await harness.Start();

        var infractionService = (IInfractionService)new InfractionService(services.GetRequiredService<IBus>(), mediator.Object);
        var now = DateTimeOffset.UtcNow.AddSeconds(1);
        var infraction = new InfractionDTO
        (
            InfractionID,
            null,
            false,
            InfractionReason,
            UserID,
            GuildID,
            ModeratorID,
            InfractionType.Ban,
            now,
            now
        );

        mediator.Setup(m => m.Send(It.IsAny<CreateInfractionRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(infraction);

        var result = await infractionService.CreateInfractionAsync(GuildID, UserID, ModeratorID, InfractionType.Ban, InfractionReason, now);

        mediator.Verify(m => m.Send(It.IsAny<CreateInfractionRequest>(), It.IsAny<CancellationToken>()), Times.Once);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, result.Error?.Message);
            Assert.That(result.Entity, Is.EqualTo(infraction));
        });
    }

    [Test]
    public async Task DoesNotAllowNegativeExpirations()
    {
        var mediator = Mock.Of<IMediator>();
        var services = new ServiceCollection().AddMassTransit(bus => DependencyInjectionTestingExtensions.AddMassTransitInMemoryTestHarness(bus))
                                              .BuildServiceProvider();

        var service = (IInfractionService)new InfractionService(services.GetRequiredService<IBus>(), mediator);

        var result = await service.CreateInfractionAsync(default, default, default, InfractionType.Ban, InfractionReason, default(DateTimeOffset));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.InstanceOf<InvalidOperationError>());
            Assert.That(result.Error!.Message, Is.EqualTo("The expiration date must be in the future."));
        });

    }

    [Test]
    public async Task DisallowsExpirationsOnPermanentTypes()
    {
        var mediator = Mock.Of<IMediator>();
        var services = new ServiceCollection().AddMassTransit(bus => DependencyInjectionTestingExtensions.AddMassTransitInMemoryTestHarness(bus))
                                              .BuildServiceProvider();

        var service = (IInfractionService)new InfractionService(services.GetRequiredService<IBus>(), mediator);

        var result = await service.CreateInfractionAsync(default, default, default, InfractionType.Kick, InfractionReason, DateTimeOffset.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.InstanceOf<InvalidOperationError>());
            Assert.That(result.Error!.Message, Is.EqualTo("Only mutes and bans can have an expiration date."));
        });
    }

    [Test]
    public async Task AllowsOmittedExpirationOnTemporaryTypes()
    {
        var mediator = new Mock<IMediator>();
        var services = new ServiceCollection().AddMassTransit(bus => DependencyInjectionTestingExtensions.AddMassTransitInMemoryTestHarness(bus))
                                              .BuildServiceProvider();

        var infractionService = (IInfractionService)new InfractionService(services.GetRequiredService<IBus>(), mediator.Object);
        var now = DateTimeOffset.UtcNow.AddSeconds(1);
        var infraction = new InfractionDTO
        (
            InfractionID,
            null,
            false,
            InfractionReason,
            UserID,
            GuildID,
            ModeratorID,
            InfractionType.Ban,
            now,
            now
        );

        mediator.Setup(m => m.Send(It.IsAny<CreateInfractionRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(infraction);

        var result = await infractionService.CreateInfractionAsync(GuildID, UserID, ModeratorID, InfractionType.Ban, InfractionReason);

        Assert.That(result.IsSuccess);
    }

    [Test]
    public async Task DispatchesExpiredRemindersAsync()
    {
        var mediator = new Mock<IMediator>();
        var services = new ServiceCollection().AddMassTransit(bus => DependencyInjectionTestingExtensions.AddMassTransitInMemoryTestHarness(bus))
                                              .BuildServiceProvider();

        var infractionService = new InfractionService(services.GetRequiredService<IBus>(), mediator.Object);
        var websocket = new Mock<WebSocket>();
        websocket.Setup(ws => ws.State).Returns(WebSocketState.Open);

        var now = DateTimeOffset.UtcNow.AddSeconds(1);
        var infraction = new InfractionDTO
        (
            InfractionID,
            null,
            false,
            InfractionReason,
            UserID,
            GuildID,
            ModeratorID,
            InfractionType.Ban,
            now,
            DateTimeOffset.MinValue
        );

        mediator.Setup(m => m.Send(It.IsAny<GetAllInfractionsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { infraction });

        await infractionService.StartAsync(CancellationToken.None);

        await Task.Delay(200);

        await infractionService.StopAsync(CancellationToken.None);

        mediator.Verify(m => m.Send(It.IsAny<GetAllInfractionsRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        websocket.Verify(ws => ws.SendAsync(It.IsAny<ReadOnlyMemory<byte>>(), WebSocketMessageType.Text, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SuccessfullyUpdatesInfraction()
    {
        var mediator = new Mock<IMediator>();
        var services = new ServiceCollection().AddMassTransit(bus => DependencyInjectionTestingExtensions.AddMassTransitInMemoryTestHarness(bus))
                                              .BuildServiceProvider();

        var infractionService = (IInfractionService)new InfractionService(services.GetRequiredService<IBus>(), mediator.Object);
        var now = DateTimeOffset.UtcNow.AddSeconds(1);
        var infraction = new InfractionDTO
        (
            InfractionID,
            null,
            false,
            InfractionReason,
            UserID,
            GuildID,
            ModeratorID,
            InfractionType.Ban,
            now,
            now
        );

        mediator.Setup(m => m.Send(It.IsAny<UpdateInfractionRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(infraction);

        var result = await infractionService.UpdateInfractionAsync(InfractionID, GuildID, default, true, default);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess);
            Assert.That(result.Entity, Is.EqualTo(infraction));
        });
    }

    // TODO: Assert that updating a reminder does not dispatch; if we set an infraction to never expire, Kobalt should never know about it afterwards

    [Test]
    public async Task DoesNotAllowEmptyUpdate()
    {
        var service = (IInfractionService) new InfractionService(Mock.Of<IBus>(), Mock.Of<IMediator>());

        var result = await service.UpdateInfractionAsync(InfractionID, GuildID, default, default, default);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.InstanceOf<InvalidOperationError>());
            Assert.That(result.Error!.Message, Is.EqualTo("You must provide at least one value to update."));
        });
    }

    [Test]
    public async Task DoesNotAllowNegativeExpirationInUpdate()
    {
        var service = (IInfractionService) new InfractionService(Mock.Of<IBus>(), Mock.Of<IMediator>());

        var result = await service.UpdateInfractionAsync(InfractionID, GuildID, default, default, DateTimeOffset.MinValue);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.InstanceOf<InvalidOperationError>());
            Assert.That(result.Error!.Message, Is.EqualTo("The expiration date must be in the future."));
        });
    }
}
