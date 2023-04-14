using NodaTime;
using Remora.Rest.Core;

namespace Kobalt.Shared.DTOs.Users;

/// <summary>
/// Represents a user.
/// </summary>
/// <param name="ID">The ID of the user.</param>
/// <param name="Timezone">The timezone of the user.</param>
/// <param name="DisplayTimezone">Whether the user wants their timezone to be displayed.</param>
/// <param name="Guilds">The guilds the user is in.</param>
public record UserDTO
(
    Snowflake ID,
    Offset? Timezone,
    bool DisplayTimezone,
    IReadOnlyList<Snowflake> Guilds
);
