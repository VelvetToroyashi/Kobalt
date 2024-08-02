using Kobalt.Infractions.Shared.DTOs;
using Kobalt.Infractions.Shared.Payloads;
using Refit;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Infractions.Shared.Interfaces;

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
        Optional<string?> reason,
        Optional<bool> isHidden,
        Optional<DateTimeOffset?> expiresAt
    );

    /// <summary>
    /// Evaluates a server's configured infraction rules, returning additionally-generated infractions, if any.
    /// </summary>
    /// <param name="guildID">The ID of the guild the rules should be evaluated on.</param>
    /// <param name="userID">The ID of the user to evaluate infractions for.</param>
    /// <returns>All additionally-generated infractions, if any.</returns>
    Task<Optional<IReadOnlyList<InfractionDTO>>> EvaluateInfractionsAsync(ulong guildID, ulong userID);

}

public interface IInfractionAPI
{
    [Put("/infractions/guilds/{guildID}")]
    public Task<InfractionResponsePayload> CreateInfractionAsync(Snowflake guildID, [Body] InfractionCreatePayload payload);

    [Get("/infractions/guilds/{guildID}")]
    public Task<IReadOnlyList<InfractionDTO>> GetGuildInfractionsAsync(Snowflake guildID, int? page = 1, int? pageSize = 10);

    [Get("/infractions/guilds/{guildID}/{id}")]
    public Task<InfractionDTO> GetGuildInfractionAsync(Snowflake guildID, int id);

    [Patch("/infractions/guilds/{guildID}/{id}")]
    public Task<InfractionDTO> UpdateInfractionAsync(Snowflake guildID, int id, [Body] InfractionUpdatePayload payload);

    [Get("/infractions/guilds/{guildID}/users/{id}")]
    public Task<IReadOnlyList<InfractionDTO>> GetInfractionsForUserAsync(Snowflake guildID, Snowflake id, [Query] [AliasAs("with_pardons")] bool withPardons = false);

    [Get("/infractions/{guildID}/rules")]
    public Task<IReadOnlyList<InfractionRuleDTO>> GetInfractionRulesAsync(Snowflake guildID);

    [Post("/infractions/{guildID}/rules")]
    public Task<InfractionRuleDTO> CreateInfractionRuleAsync(Snowflake guildID, [Body] InfractionRuleDTO rule);

    [Patch("/infractions/{guildID}/rules/{id}")]
    public Task<InfractionRuleDTO> UpdateInfractionRuleAsync(Snowflake guildID, int id, [Body] InfractionRuleDTO rule);

    [Delete("/infractions/{guildID}/rules/{id}")]
    public Task DeleteInfractionRuleAsync(Snowflake guildID, int id);

    [Get("/infractions/{guildID}/rules/{id}")]
    public Task<InfractionRuleDTO> GetInfractionRuleAsync(Snowflake guildID, int id);
}
