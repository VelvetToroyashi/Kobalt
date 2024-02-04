using Kobalt.Bot.Data.DTOs;
using Kobalt.Bot.Data.MediatR.Guilds;
using Kobalt.Bot.Services;
using MediatR;
using NSubstitute;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Bot.Tests.Services;

public class AntiRaidV2ServiceTests
{
    private static readonly GuildAntiRaidConfigDTO DefaultConfig = new
    (
        default,
        default,
        default,
        default,
        default,
        default,
        default,
        default,
        default,
        default,
        default,
        default
    );

    /// <summary>
    /// Tests that the HandleAsync method returns success when the configuration is disabled.
    /// </summary>
    [Test]
    public async Task HandleAsync_WhenConfigIsDisabled_ReturnsSuccess()
    {
        var mediator = Substitute.For<IMediator>();

        mediator.Send(new GetGuild.AntiRaidConfigRequest(Arg.Any<Snowflake>()), Arg.Any<CancellationToken>())
            .Returns(Result<GuildAntiRaidConfigDTO>.FromSuccess(DefaultConfig));

        var service = new AntiRaidV2Service(null!, mediator, null!, TimeProvider.System);
        var result = await service.HandleAsync(Substitute.For<IGuildMemberAdd>());
        
        Assert.That(result.IsSuccess, Is.True);
    }

    /// <summary>
    /// Tests that the raid state assigns a base score to non-suspicious users correctly.
    /// </summary>
    [Test]
    public void RaidStateAppliesBaseScoreCorrectly()
    {
        var state = new RaidState(TimeProvider.System);
        var config = DefaultConfig with { BaseJoinScore = 5 };

        state.AddUser(Substitute.For<IUser>(), DateTimeOffset.UtcNow, config);

        var user = state._users[0];

        Assert.That(user.ThreatScore, Is.EqualTo(5));
    }

    /// <summary>
    /// Tests that the raid state applies a score for users that join too quickly after another user.
    /// </summary>
    [Test]
    public void RaidStateCalculatesSuspiciousJoinDeltaCorrectly()
    {
        var state = new RaidState(TimeProvider.System);
        var config = DefaultConfig with { JoinVelocityScore = 10, LastJoinBufferPeriod = TimeSpan.FromSeconds(1) };

        state.AddUser(Substitute.For<IUser>(), DateTimeOffset.UtcNow, config);
        state.AddUser(Substitute.For<IUser>(), DateTimeOffset.UtcNow, config);

        var user = state._users[1];

        Assert.That(user.ThreatScore, Is.EqualTo(10));
    }

    /// <summary>
    /// Tests that the raid state applies the correct score when a user does not have an avatar.
    /// </summary>
    [Test]
    public void RaidStateCalculatesNoAvatarScoreCorrectly()
    {
        var state = new RaidState(TimeProvider.System);
        var config = DefaultConfig with { NoAvatarScore = 10 };

        state.AddUser(Substitute.For<IUser>(), DateTimeOffset.UtcNow, config);

        var user = state._users[0];

        Assert.That(user.ThreatScore, Is.EqualTo(10));
    }

    /// <summary>
    /// Tests that the raid state returns suspicious users correctly.
    /// Users are considered suspicious if their threat score is greater than the base score.
    /// </summary>
    [Test]
    public void RaidStateCalculatesReturnsSuspiciousUsersCorrectly()
    {
        var state = new RaidState(TimeProvider.System);
        var config = DefaultConfig with { JoinVelocityScore = 10, LastJoinBufferPeriod = TimeSpan.FromSeconds(1), AntiRaidCooldownPeriod = TimeSpan.FromSeconds(10) };

        state.AddUser(Substitute.For<IUser>(), DateTimeOffset.UtcNow, config);
        state.AddUser(Substitute.For<IUser>(), DateTimeOffset.UtcNow, config);
        state.AddUser(Substitute.For<IUser>(), DateTimeOffset.UtcNow, config);
        state.AddUser(Substitute.For<IUser>(), DateTimeOffset.UtcNow.AddSeconds(1), config);

        var suspiciousUsers = state.GetSuspiciousUsers(config).ToArray();

        Assert.That(suspiciousUsers.Length, Is.EqualTo(2));
    }
}