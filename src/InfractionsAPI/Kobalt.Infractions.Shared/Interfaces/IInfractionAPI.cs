using Kobalt.Infractions.Shared.DTOs;
using Kobalt.Infractions.Shared.Payloads;
using Refit;
using Remora.Rest.Core;

namespace Kobalt.Infractions.Shared.Interfaces;

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
