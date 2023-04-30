using MediatR;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;

namespace Kobalt.Shared.Mediator.Moderation;

/// <summary>
/// Represents the intention to ban a user; the user may or not have already been banned.
/// </summary>
/// <param name="GuildID">The ID of the guild of the event.</param>
/// <param name="Target">The target of the ban.</param>
/// <param name="Moderator">The moderator of the ban.</param>
/// <param name="Reason">The reason for the ban.</param>
/// <param name="Expiration">Then the ban expires, if ever.</param>
public record BanUserNotification(Snowflake GuildID, IUser Target, IUser Moderator, string Reason, TimeSpan? Duration) : INotification;
