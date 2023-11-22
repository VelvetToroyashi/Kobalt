namespace Kobalt.Bot.Data.DTOs;

public class RoleMenuDTO(int Id, string Name, string Description, int MaxSelections, IReadOnlyList<RoleMenuOptionDTO> Options)
{
    public int Id { get; init; } = Id;
    public string Name { get; init; } = Name;
    public string Description { get; init; } = Description;
    public int MaxSelections { get; init; } = MaxSelections;
    public IReadOnlyList<RoleMenuOptionDTO> Options { get; init; } = Options;
}