using Kobalt.Infractions.Infrastructure.Mediator.DTOs;
using Kobalt.Infractions.Shared;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Infractions.Infrastructure.Interfaces;

public interface IInfractionService
{
    /// <summary>
    /// Creates a new infraction.
    /// </summary>
    /// <param name="guildID">The ID of the guild where the infraction took place.</param>
    /// <param name="userID">The ID of the user who received the infraction.</param>
    /// <param name="moderatorID">The ID of the moderator who created the infraction.</param>
    /// <param name="type">The type of the infraction.</param>
    /// <param name="reason">The reason for the infraction.</param>
    /// <param name="expiresAt">When the infraction expires, if applicable.</param>
    /// <param name="referencedID">The ID of the referenced infraction, if applicable.</param>
    /// <returns>The newly created infraction, or an error.</returns>
    Task<Result<InfractionDTO>> CreateInfractionAsync
    (
        ulong guildID,
        ulong userID,
        ulong moderatorID,
        InfractionType type,
        string reason,
        DateTimeOffset? expiresAt = null,
        int? referencedID = null
    );
    
    /// <summary>
    /// Updates an existing infraction with the provided changes.
    /// </summary>
    /// <param name="id">The ID of the infraction to update.</param>
    /// <param name="guildID">The ID of the guild where the infraction took place.</param>
    /// <param name="reason">The updated reason for the infraction, if applicable.</param>
    /// <param name="isHidden">Whether the updated infraction is hidden.</param>
    /// <param name="expiresAt">When the updated infraction expires, if applicable.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation of updating the infraction.</returns>
    Task<Result<InfractionDTO>> UpdateInfractionAsync
    (
        int id,
        ulong guildID,
        Optional<string> reason,
        Optional<bool> isHidden,
        Optional<DateTimeOffset?> expiresAt
    );
    


}
