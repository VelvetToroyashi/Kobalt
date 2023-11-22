namespace Kobalt.Bot.Data.DTOs;

public class RoleMenuDTO(int Id, string Name, string Description, int MaxSelections, IReadOnlyList<RoleMenuOptionDTO> Options)
{
    public int Id { get; set; } = Id;
    public string Name { get; set; } = Name;
    public string Description { get; set; } = Description;
    public int MaxSelections { get; set; } = MaxSelections;
    public IReadOnlyList<RoleMenuOptionDTO> Options { get; set; } = Options;
}