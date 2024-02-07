using Remora.Rest.Core;

namespace Kobalt.Shared.Models;

/// <summary>
/// Represents the payload for updating user settings.
/// </summary>
/// <param name="Timezone">The user's new timezone.</param>
/// <param name="DisplayTimezone">Whether to publicly display said timezone.</param>
public record UserSettingsUpdatePayload(Optional<string> Timezone, Optional<bool> DisplayTimezone);