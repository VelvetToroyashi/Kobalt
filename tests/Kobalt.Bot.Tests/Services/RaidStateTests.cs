using Kobalt.Bot.Data.DTOs;
using Kobalt.Bot.Services;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NUnit.Framework;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;

namespace Kobalt.Bot.Tests.Services;

[TestFixture]
public class RaidStateTests
{
    private FakeTimeProvider _fakeTimeProvider;
    private GuildAntiRaidConfigDTO _defaultConfig;

    [SetUp]
    public void Setup()
    {
        _fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _defaultConfig = new GuildAntiRaidConfigDTO
        (
            IsEnabled: true,
            ThreatScoreThreshold: 10,
            BaseJoinScore: 1,
            MinimumAccountAge: TimeSpan.FromHours(1),
            MinimumAgeScore: 3,
            NoAvatarScore: 2,
            JoinVelocityScore: 2,
            LastJoinBufferPeriod: TimeSpan.FromSeconds(10),
            AntiRaidCooldownPeriod: TimeSpan.FromMinutes(1),
            MinimumAccountAgeBypass: TimeSpan.FromDays(7),
            AccountFlagsBypass: null // No flag bypass by default
        );
    }

    private IUser CreateMockUser(ulong id, DateTimeOffset creationDate, string? avatarHash = "has_avatar")
    {
        var user = Substitute.For<IUser>();
        user.ID.Returns(new Snowflake(id, creationDate));
        user.Avatar.Returns(avatarHash == null ? null : new UserAvatar(avatarHash, false, false, false, false, false));
        user.Flags.Returns(UserFlags.None);
        return user;
    }

    [Test]
    public void AddUser_FirstUser_GetsBaseScore()
    {
        var raidState = new RaidState(_fakeTimeProvider);
        var user = CreateMockUser(1, _fakeTimeProvider.GetUtcNow() - TimeSpan.FromDays(2));

        raidState.AddUser(user, _fakeTimeProvider.GetUtcNow(), _defaultConfig);

        Assert.That(raidState._users.Count, Is.EqualTo(1));
        Assert.That(raidState._users[0].ThreatScore, Is.EqualTo(_defaultConfig.BaseJoinScore));
    }

    [Test]
    public void AddUser_NoAvatar_AddsNoAvatarScore()
    {
        var raidState = new RaidState(_fakeTimeProvider);
        var user = CreateMockUser(1, _fakeTimeProvider.GetUtcNow() - TimeSpan.FromDays(2), avatarHash: null);

        raidState.AddUser(user, _fakeTimeProvider.GetUtcNow(), _defaultConfig);

        Assert.That(raidState._users[0].ThreatScore, Is.EqualTo(_defaultConfig.BaseJoinScore + _defaultConfig.NoAvatarScore));
    }

    [Test]
    public void AddUser_YoungAccount_AddsMinimumAgeScore()
    {
        var raidState = new RaidState(_fakeTimeProvider);
        var userCreation = _fakeTimeProvider.GetUtcNow() - TimeSpan.FromMinutes(30); // Younger than MinAccountAge
        var user = CreateMockUser(1, userCreation);

        raidState.AddUser(user, _fakeTimeProvider.GetUtcNow(), _defaultConfig);

        Assert.That(raidState._users[0].ThreatScore, Is.EqualTo(_defaultConfig.BaseJoinScore + _defaultConfig.MinimumAgeScore));
    }

    [Test]
    public void AddUser_RapidJoin_AddsJoinVelocityScore()
    {
        var raidState = new RaidState(_fakeTimeProvider);
        var user1 = CreateMockUser(1, _fakeTimeProvider.GetUtcNow() - TimeSpan.FromDays(2));
        var user2 = CreateMockUser(2, _fakeTimeProvider.GetUtcNow() - TimeSpan.FromDays(2));

        raidState.AddUser(user1, _fakeTimeProvider.GetUtcNow(), _defaultConfig);
        _fakeTimeProvider.Advance(TimeSpan.FromSeconds(5)); // Less than LastJoinBufferPeriod
        raidState.AddUser(user2, _fakeTimeProvider.GetUtcNow(), _defaultConfig);

        Assert.That(raidState._users[1].ThreatScore, Is.EqualTo(_defaultConfig.BaseJoinScore + _defaultConfig.JoinVelocityScore));
    }

    [Test]
    public void AddUser_SlowJoin_DoesNotAddJoinVelocityScore()
    {
        var raidState = new RaidState(_fakeTimeProvider);
        var user1 = CreateMockUser(1, _fakeTimeProvider.GetUtcNow() - TimeSpan.FromDays(2));
        var user2 = CreateMockUser(2, _fakeTimeProvider.GetUtcNow() - TimeSpan.FromDays(2));

        raidState.AddUser(user1, _fakeTimeProvider.GetUtcNow(), _defaultConfig);
        _fakeTimeProvider.Advance(TimeSpan.FromSeconds(15)); // More than LastJoinBufferPeriod
        raidState.AddUser(user2, _fakeTimeProvider.GetUtcNow(), _defaultConfig);

        Assert.That(raidState._users[1].ThreatScore, Is.EqualTo(_defaultConfig.BaseJoinScore));
    }

    [Test]
    public void AddUser_AccountAgeBypass_ResetsScoreToBase()
    {
        var raidState = new RaidState(_fakeTimeProvider);
        // Young account, no avatar, rapid join potential - high score
        var youngUserNoAvatar = CreateMockUser(1, _fakeTimeProvider.GetUtcNow() - TimeSpan.FromMinutes(10), avatarHash: null);
        var configWithBypass = _defaultConfig with { MinimumAccountAgeBypass = TimeSpan.FromMinutes(5) }; // Bypass if older than 5 mins

        raidState.AddUser(youngUserNoAvatar, _fakeTimeProvider.GetUtcNow(), configWithBypass);

        // Even though account is "young" (10min) by _defaultConfig.MinimumAccountAge (1hr),
        // it's older than MinimumAccountAgeBypass (5min), so score should be BaseJoinScore.
        Assert.That(raidState._users[0].ThreatScore, Is.EqualTo(configWithBypass.BaseJoinScore));
    }

    [Test]
    public void AddUser_AccountAgeDoesNotBypassIfYoungerThanBypassThreshold()
    {
        var raidState = new RaidState(_fakeTimeProvider);
        var veryYoungUser = CreateMockUser(1, _fakeTimeProvider.GetUtcNow() - TimeSpan.FromMinutes(2), avatarHash: null);
        var configWithBypass = _defaultConfig with { MinimumAccountAgeBypass = TimeSpan.FromMinutes(5) };

        raidState.AddUser(veryYoungUser, _fakeTimeProvider.GetUtcNow(), configWithBypass);

        // Account is younger (2min) than MinimumAccountAgeBypass (5min), so bypass doesn't apply.
        // Score should include NoAvatarScore and MinimumAgeScore.
        var expectedScore = configWithBypass.BaseJoinScore + configWithBypass.NoAvatarScore + configWithBypass.MinimumAgeScore;
        Assert.That(raidState._users[0].ThreatScore, Is.EqualTo(expectedScore));
    }


    [Test]
    public void AddUser_UserFlagsBypass_ResetsScoreToBase()
    {
        var raidState = new RaidState(_fakeTimeProvider);
        var userWithFlags = CreateMockUser(1, _fakeTimeProvider.GetUtcNow() - TimeSpan.FromMinutes(10), avatarHash: null);
        userWithFlags.Flags.Returns(UserFlags.Partner | UserFlags.VerifiedBotDeveloper);

        var configWithFlagBypass = _defaultConfig with { AccountFlagsBypass = UserFlags.Partner | UserFlags.VerifiedBotDeveloper };

        raidState.AddUser(userWithFlags, _fakeTimeProvider.GetUtcNow(), configWithFlagBypass);

        Assert.That(raidState._users[0].ThreatScore, Is.EqualTo(configWithFlagBypass.BaseJoinScore));
    }

    [Test]
    public void AddUser_UserFlagsDoNotBypassIfNotAllPresent()
    {
        var raidState = new RaidState(_fakeTimeProvider);
        var userWithSomeFlags = CreateMockUser(1, _fakeTimeProvider.GetUtcNow() - TimeSpan.FromMinutes(10), avatarHash: null);
        userWithSomeFlags.Flags.Returns(UserFlags.Partner); // Only has one of the bypass flags

        var configWithFlagBypass = _defaultConfig with { AccountFlagsBypass = UserFlags.Partner | UserFlags.VerifiedBotDeveloper };

        raidState.AddUser(userWithSomeFlags, _fakeTimeProvider.GetUtcNow(), configWithFlagBypass);

        var expectedScore = configWithFlagBypass.BaseJoinScore + configWithFlagBypass.NoAvatarScore + configWithFlagBypass.MinimumAgeScore;
        Assert.That(raidState._users[0].ThreatScore, Is.EqualTo(expectedScore));
    }


    [Test]
    public void IsRaid_TriggersWhenScoreReachesThreshold()
    {
        var raidState = new RaidState(_fakeTimeProvider);
        var config = _defaultConfig with { ThreatScoreThreshold = 5, BaseJoinScore = 2, NoAvatarScore = 1, MinimumAgeScore = 2 };
        // Each user: Base (2) + NoAvatar (1) + MinAge (2) = 5

        var user1 = CreateMockUser(1, _fakeTimeProvider.GetUtcNow() - TimeSpan.FromMinutes(10), avatarHash: null);
        raidState.AddUser(user1, _fakeTimeProvider.GetUtcNow(), config);

        Assert.That(raidState.IsRaid(config), Is.True);
    }

    [Test]
    public void IsRaid_DoesNotTriggerBelowThreshold()
    {
        var raidState = new RaidState(_fakeTimeProvider);
        var config = _defaultConfig with { ThreatScoreThreshold = 6, BaseJoinScore = 2, NoAvatarScore = 1, MinimumAgeScore = 2 };

        var user1 = CreateMockUser(1, _fakeTimeProvider.GetUtcNow() - TimeSpan.FromMinutes(10), avatarHash: null);
        raidState.AddUser(user1, _fakeTimeProvider.GetUtcNow(), config); // Score = 5

        Assert.That(raidState.IsRaid(config), Is.False);
    }

    [Test]
    public void MarkUsersHandled_CorrectlyFlagsUsers()
    {
        var raidState = new RaidState(_fakeTimeProvider);
        var user1 = CreateMockUser(1, _fakeTimeProvider.GetUtcNow() - TimeSpan.FromDays(1));
        var user2 = CreateMockUser(2, _fakeTimeProvider.GetUtcNow() - TimeSpan.FromDays(1));
        raidState.AddUser(user1, _fakeTimeProvider.GetUtcNow(), _defaultConfig);
        raidState.AddUser(user2, _fakeTimeProvider.GetUtcNow(), _defaultConfig);

        raidState.MarkUsersHandled(new[] { user1.ID });

        Assert.That(raidState._users.First(u => u.User.ID == user1.ID).Handled, Is.True);
        Assert.That(raidState._users.First(u => u.User.ID == user2.ID).Handled, Is.False);
    }

    [Test]
    public void GetSuspiciousUsers_ReturnsUnhandeledUsersAboveBaseScore()
    {
        var raidState = new RaidState(_fakeTimeProvider);
        var config = _defaultConfig with { BaseJoinScore = 1, NoAvatarScore = 2 }; // Suspicious if score > 1

        var user1 = CreateMockUser(10, _fakeTimeProvider.GetUtcNow() - TimeSpan.FromDays(1)); // Score = 1 (Base)
        var user2 = CreateMockUser(20, _fakeTimeProvider.GetUtcNow() - TimeSpan.FromDays(1), avatarHash: null); // Score = 1+2=3 (Base + NoAvatar)
        var user3 = CreateMockUser(30, _fakeTimeProvider.GetUtcNow() - TimeSpan.FromDays(1), avatarHash: null); // Score = 3

        raidState.AddUser(user1, _fakeTimeProvider.GetUtcNow(), config);
        raidState.AddUser(user2, _fakeTimeProvider.GetUtcNow(), config);
        raidState.AddUser(user3, _fakeTimeProvider.GetUtcNow(), config);

        raidState.MarkUsersHandled(new[] { user3.ID });

        var suspiciousUsers = raidState.GetSuspiciousUsers(config).ToArray();

        Assert.That(suspiciousUsers.Length, Is.EqualTo(1));
        Assert.That(suspiciousUsers[0].ID, Is.EqualTo(user2.ID));
    }

    [Test]
    public void ClearState_RemovesOldUsers()
    {
        var raidState = new RaidState(_fakeTimeProvider);
        var config = _defaultConfig with { AntiRaidCooldownPeriod = TimeSpan.FromMinutes(5) };

        var user1JoinTime = _fakeTimeProvider.GetUtcNow();
        var user1 = CreateMockUser(1, user1JoinTime - TimeSpan.FromDays(1));
        raidState.AddUser(user1, user1JoinTime, config);

        _fakeTimeProvider.Advance(TimeSpan.FromMinutes(3));
        var user2JoinTime = _fakeTimeProvider.GetUtcNow();
        var user2 = CreateMockUser(2, user2JoinTime - TimeSpan.FromDays(1));
        raidState.AddUser(user2, user2JoinTime, config);

        Assert.That(raidState._users.Count, Is.EqualTo(2));

        // Advance time so user1 is outside cooldown, user2 is still inside
        _fakeTimeProvider.Advance(TimeSpan.FromMinutes(3)); // Total 6 mins past user1 join, 3 mins past user2 join

        // Trigger ClearState by calling IsRaid (or GetSuspiciousUsers)
        raidState.IsRaid(config);

        Assert.That(raidState._users.Count, Is.EqualTo(1));
        Assert.That(raidState._users[0].User.ID, Is.EqualTo(user2.ID));
    }

    [Test]
    public void ClearState_ClearsAllUsersIfLastJoinIsPastCooldown()
    {
        var raidState = new RaidState(_fakeTimeProvider);
        var config = _defaultConfig with { AntiRaidCooldownPeriod = TimeSpan.FromMinutes(5) };

        var user1JoinTime = _fakeTimeProvider.GetUtcNow();
        var user1 = CreateMockUser(1, user1JoinTime - TimeSpan.FromDays(1));
        raidState.AddUser(user1, user1JoinTime, config);

        _fakeTimeProvider.Advance(TimeSpan.FromMinutes(1));
        var user2JoinTime = _fakeTimeProvider.GetUtcNow();
        var user2 = CreateMockUser(2, user2JoinTime - TimeSpan.FromDays(1));
        raidState.AddUser(user2, user2JoinTime, config);

        // Advance time so all users are outside cooldown
        _fakeTimeProvider.Advance(TimeSpan.FromMinutes(5)); // user2 joined 5 mins ago, now it's past cooldown.
                                                            // user1 joined 6 mins ago.

        raidState.IsRaid(config); // Trigger ClearState

        Assert.That(raidState._users.Count, Is.EqualTo(0));
    }
}
