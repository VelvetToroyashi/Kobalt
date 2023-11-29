using Kobalt.Bot.Data.Entities.RoleMenus;

namespace Kobalt.Bot.Data.DTOs;

public record RoleMenuDTO
(
    int Id,
    string Name,
    string Description,
    int MaxSelections,
    IReadOnlyList<RoleMenuOptionDTO> Options
)
{
    public static RoleMenuDTO FromEntity(RoleMenuEntity entity)
    {
        return new RoleMenuDTO(
            entity.Id,
            entity.Name,
            entity.Description,
            entity.MaxSelections,
            entity.Options.Select(RoleMenuOptionDTO.FromEntity).ToList()
        );
    }
}