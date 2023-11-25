using Remora.Rest.Core;

namespace Kobalt.Bot.Data.DTOs;

public record RoleMenuOptionDTO(int Id, string Name, string Description, Snowflake RoleID, IReadOnlyList<Snowflake> MutuallyInclusiveRoleIDs, IReadOnlyList<Snowflake> MutuallyExclusiveRoleIDs);