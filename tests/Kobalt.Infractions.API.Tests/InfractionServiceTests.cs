using Kobalt.Infractions.API.Services;
using Kobalt.Infractions.Data.MediatR;
using Kobalt.Infractions.Shared;
using Kobalt.Infractions.Shared.DTOs;
using Kobalt.Infractions.Shared.Interfaces;
using MassTransit;
using MassTransit.Testing;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
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
        var mediator = Substitute.For<IMediator>();

        var infractionService = (IInfractionService)new InfractionService(Substitute.For<IBus>(), mediator);
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
        
        
        mediator.Send(default!, default).ReturnsForAnyArgs(infraction);

        var result = await infractionService.CreateInfractionAsync(GuildID, UserID, ModeratorID, InfractionType.Ban, InfractionReason, now);

        _ = mediator.Received(1).Send(Arg.Any<CreateInfractionRequest>(), default);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, result.Error?.Message);
            Assert.That(result.Entity, Is.EqualTo(infraction));
        });
    }

    [Test]
    public async Task DoesNotAllowNegativeExpirations()
    {
        var mediator = Substitute.For<IMediator>();
        var service = (IInfractionService)new InfractionService(Substitute.For<IBus>(), mediator);

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
        var mediator = Substitute.For<IMediator>();
        var service = (IInfractionService)new InfractionService(Substitute.For<IBus>(), mediator);

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
        var mediator = Substitute.For<IMediator>();

        var infractionService = (IInfractionService)new InfractionService(Substitute.For<IBus>(), mediator);
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

        mediator.Send(default!, default).ReturnsForAnyArgs(infraction);

        var result = await infractionService.CreateInfractionAsync(GuildID, UserID, ModeratorID, InfractionType.Ban, InfractionReason);

        Assert.That(result.IsSuccess);
    }

    [Test]
    public async Task SuccessfullyUpdatesInfraction()
    {
        var mediator = Substitute.For<IMediator>();

        var infractionService = (IInfractionService)new InfractionService(Substitute.For<IBus>(), mediator);
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

        mediator.Send(default!, default).ReturnsForAnyArgs(infraction);

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
        var service = (IInfractionService) new InfractionService(Substitute.For<IBus>(), Substitute.For<IMediator>());

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
        var service = (IInfractionService) new InfractionService(Substitute.For<IBus>(), Substitute.For<IMediator>());

        var result = await service.UpdateInfractionAsync(InfractionID, GuildID, default, default, DateTimeOffset.MinValue);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.InstanceOf<InvalidOperationError>());
            Assert.That(result.Error!.Message, Is.EqualTo("The expiration date must be in the future."));
        });
    }
}
