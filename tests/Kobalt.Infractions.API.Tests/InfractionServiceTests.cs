using System.Net.WebSockets;
using System.Text.Json;
using Kobalt.Infractions.API.Services;
using Kobalt.Infractions.Data.Mediator;
using Kobalt.Infractions.Infrastructure.Interfaces;
using Kobalt.Infractions.Infrastructure.Mediator;
using Kobalt.Infractions.Infrastructure.Mediator.DTOs;
using Kobalt.Infractions.Shared;
using Kobalt.Shared.Services;
using Mediator;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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
        var jsonOptions = Options.Create(JsonSerializerOptions.Default);
        var socketManager = new WebsocketManagerService(NullLogger<WebsocketManagerService>.Instance);
        var infractionService = (IInfractionService)new InfractionService(mediator.Object, jsonOptions, socketManager);

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
        
        var result = await infractionService.CreateInfractionAsync(GuildID, UserID, ModeratorID, InfractionType.Ban, InfractionReason, now, null);
        
        mediator.Verify(m => m.Send(It.IsAny<CreateInfractionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        
        Assert.IsTrue(result.IsSuccess, result.Error?.Message);
        Assert.AreEqual(infraction, result.Entity);
    }

    [Test]
    public async Task DoesNotAllowNegativeExpirations()
    {
        var mediator = Mock.Of<IMediator>();
        var jsonOptions = Options.Create(JsonSerializerOptions.Default);
        var socketManager = new WebsocketManagerService(NullLogger<WebsocketManagerService>.Instance);

        var service = (IInfractionService)new InfractionService(mediator, jsonOptions, socketManager);

        var result = await service.CreateInfractionAsync(default, default, default, InfractionType.Ban, default, default(DateTimeOffset));
        
        Assert.IsFalse(result.IsSuccess);
        Assert.IsInstanceOf<InvalidOperationError>(result.Error);
        Assert.AreEqual("The expiration date must be in the future.", result.Error.Message);
    }

    [Test]
    public async Task DisallowsExpirationsOnPermanentTypes()
    {
        var mediator = Mock.Of<IMediator>();
        var jsonOptions = Options.Create(JsonSerializerOptions.Default);
        var socketManager = new WebsocketManagerService(NullLogger<WebsocketManagerService>.Instance);

        var service = (IInfractionService)new InfractionService(mediator, jsonOptions, socketManager);

        var result = await service.CreateInfractionAsync(default, default, default, InfractionType.Kick, default, DateTimeOffset.MaxValue);

        Assert.IsFalse(result.IsSuccess);
        Assert.IsInstanceOf<InvalidOperationError>(result.Error);
        Assert.AreEqual("Only mutes and bans can have an expiration date.", result.Error.Message);
    }

    [Test]
    public async Task AllowsOmittedExpirationOnTemporaryTypes()
    {
        var mediator = new Mock<IMediator>();
        var jsonOptions = Options.Create(JsonSerializerOptions.Default);
        var socketManager = new WebsocketManagerService(NullLogger<WebsocketManagerService>.Instance);
        var infractionService = (IInfractionService)new InfractionService(mediator.Object, jsonOptions, socketManager);

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

        Assert.IsTrue(result.IsSuccess);
    }

    [Test]
    public async Task DispatchesExpiredRemindersAsync()
    {
        var mediator = new Mock<IMediator>();
        var jsonOptions = Options.Create(JsonSerializerOptions.Default);
        var socketManager = new WebsocketManagerService(NullLogger<WebsocketManagerService>.Instance);
        var infractionService = new InfractionService(mediator.Object, jsonOptions, socketManager);
        var websocket = new Mock<WebSocket>();
        websocket.Setup(ws => ws.State).Returns(WebSocketState.Open);

        socketManager.AddClient(websocket.Object, true);
        
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
        var jsonOptions = Options.Create(JsonSerializerOptions.Default);
        var socketManager = new WebsocketManagerService(NullLogger<WebsocketManagerService>.Instance);
        var infractionService = (IInfractionService)new InfractionService(mediator.Object, jsonOptions, socketManager);

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
        
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(infraction, result.Entity);
    }
    
    // TODO: Assert that updating a reminder does not dispatch; if we set an infraction to never expire, Kobalt should never know about it afterwards

    [Test]
    public async Task DoesNotAllowEmptyUpdate()
    {
        var service = (IInfractionService) new InfractionService(Mock.Of<IMediator>(), Options.Create(JsonSerializerOptions.Default), new(NullLogger<WebsocketManagerService>.Instance));

        var result = await service.UpdateInfractionAsync(InfractionID, GuildID, default, default, default);
        
        Assert.False(result.IsSuccess);
        Assert.IsInstanceOf<InvalidOperationError>(result.Error);
        Assert.AreEqual("You must provide at least one value to update.", result.Error.Message);
    }

    [Test]
    public async Task DoesNotAllowNegativeExpirationInUpdate()
    {
        var service = (IInfractionService) new InfractionService(Mock.Of<IMediator>(), Options.Create(JsonSerializerOptions.Default), new(NullLogger<WebsocketManagerService>.Instance));

        var result = await service.UpdateInfractionAsync(InfractionID, GuildID, default, default, DateTimeOffset.MinValue);
        
        Assert.False(result.IsSuccess);
        Assert.IsInstanceOf<InvalidOperationError>(result.Error);
        Assert.AreEqual("The expiration date must be in the future.", result.Error.Message);
    }
}
