using Kobalt.Bot.Data.Entities.RoleMenus;
using Remora.Rest.Core;

namespace Kobalt.Bot.Data.DTOs;

public record RoleMenuDTO
(
    int Id,
    string Name,
    string Description,
    Snowflake GuildID,
    Snowflake ChannelID,
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
            entity.GuildID,
            entity.ChannelID,
            entity.MaxSelections,
            entity.Options.Select(RoleMenuOptionDTO.FromEntity).ToList()
        );
    }
}