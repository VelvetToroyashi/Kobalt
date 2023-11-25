namespace Kobalt.Bot.Data.DTOs;

public record RoleMenuDTO(int Id, string Name, string Description, int MaxSelections, IReadOnlyList<RoleMenuOptionDTO> Options);