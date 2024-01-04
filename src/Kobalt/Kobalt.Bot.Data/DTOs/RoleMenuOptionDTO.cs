using Kobalt.Bot.Data.Entities.RoleMenus;
using Remora.Rest.Core;

namespace Kobalt.Bot.Data.DTOs;

public record RoleMenuOptionDTO
(
    int Id,
    string Name,
    string Description,
    Snowflake RoleID,
    IReadOnlyList<Snowflake> MutuallyInclusiveRoleIDs,
    IReadOnlyList<Snowflake> MutuallyExclusiveRoleIDs
)
{
    public static RoleMenuOptionDTO FromEntity(RoleMenuOptionEntity entity)
    {
        return new RoleMenuOptionDTO(
            entity.Id,
            entity.Name,
            entity.Description,
            entity.RoleID,
            entity.MutuallyInclusiveRoles,
            entity.MutuallyExclusiveRoles
        );
    }
}