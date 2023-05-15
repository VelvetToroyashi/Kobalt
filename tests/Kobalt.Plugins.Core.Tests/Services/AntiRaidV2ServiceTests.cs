using Kobalt.Plugins.Core.Data.DTOs;
using Kobalt.Plugins.Core.Data.Mediator;
using Kobalt.Plugins.Core.Services;
using MediatR;
using Moq;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Plugins.Core.Tests.Services;

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

    [Test]
    public async Task HandleAsync_WhenConfigIsDisabled_ReturnsSuccess()
    {
        var mediator = new Mock<IMediator>();

        mediator.Setup(m => m.Send(new GetGuild.AntiRaidConfigRequest(It.IsAny<Snowflake>()), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GuildAntiRaidConfigDTO>.FromSuccess(DefaultConfig));

        var service = new AntiRaidV2Service(null!, mediator.Object, null!);
        var result = await service.HandleAsync(Mock.Of<IGuildMemberAdd>());

        Assert.That(result.IsSuccess, Is.True);
    }

    /// <summary>
    /// Asserts that the raid state (what tracks users) assigns a base score to non-supicious users correctly.
    /// </summary>
    [Test]
    public void RaidStateAppliesBaseScoreCorrectly()
    {
        var state = new RaidState();
        var config = DefaultConfig with { BaseJoinScore = 5 };

        state.AddUser(Mock.Of<IUser>(), DateTimeOffset.UtcNow, config);

        var user = state._users[0];

        Assert.That(user.ThreatScore, Is.EqualTo(5));
    }

    /// <summary>
    /// Asserts that the raid state applies a score for users that join too quickly after another user.
    /// </summary>
    [Test]
    public void RaidStateCalculatesSuspiciousJoinDeltaCorrectly()
    {
        var state = new RaidState();
        var config = DefaultConfig with { JoinVelocityScore = 10, LastJoinBufferPeriod = TimeSpan.FromSeconds(1) };

        state.AddUser(Mock.Of<IUser>(), DateTimeOffset.UtcNow, config);
        state.AddUser(Mock.Of<IUser>(), DateTimeOffset.UtcNow, config);

        var user = state._users[1];

        Assert.That(user.ThreatScore, Is.EqualTo(10));
    }

    /// <summary>
    /// Tests that the raid state applies the correct score when a user does not have an avatar.
    /// </summary>
    [Test]
    public void RaidStateCalculatesNoAvatarScoreCorrectly()
    {
        var state = new RaidState();
        var config = DefaultConfig with { NoAvatarScore = 10 };

        state.AddUser(Mock.Of<IUser>(), DateTimeOffset.UtcNow, config);

        var user = state._users[0];

        Assert.That(user.ThreatScore, Is.EqualTo(10));
    }

    /// <summary>
    /// Tests that the raid state returns suspicious users correctly.
    /// </summary>
    /// <remarks>
    /// Users are considered suspicious if their threat score is greater than the base score.
    /// </remarks>
    [Test]
    public void RaidStateCalculatesReturnsSuspiciousUsersCorrectly()
    {
        var state = new RaidState();
        var config = DefaultConfig with { JoinVelocityScore = 10, LastJoinBufferPeriod = TimeSpan.FromSeconds(1), AntiRaidCooldownPeriod = TimeSpan.FromSeconds(10) };

        state.AddUser(Mock.Of<IUser>(), DateTimeOffset.UtcNow, config);
        state.AddUser(Mock.Of<IUser>(), DateTimeOffset.UtcNow, config);
        state.AddUser(Mock.Of<IUser>(), DateTimeOffset.UtcNow, config);
        state.AddUser(Mock.Of<IUser>(), DateTimeOffset.UtcNow.AddSeconds(1), config);

        var suspiciousUsers = state.GetSuspiciousUsers(config).ToArray();

        Assert.That(suspiciousUsers.Length, Is.EqualTo(2));
    }

}
