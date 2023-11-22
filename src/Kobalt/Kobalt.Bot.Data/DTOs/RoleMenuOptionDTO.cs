using Remora.Rest.Core;

namespace Kobalt.Bot.Data.DTOs;

public class RoleMenuOptionDTO(int Id, string Name, string Description, Snowflake RoleID, IReadOnlyList<Snowflake> MutuallyInclusiveRoleIDs, IReadOnlyList<Snowflake> MutuallyExclusiveRoleIDs)
{
    public int Id { get; init; } = Id;
    public string Name { get; init; } = Name;
    public string Description { get; init; } = Description;
    public Snowflake RoleID { get; init; } = RoleID;
    public IReadOnlyList<Snowflake> MutuallyInclusiveRoleIDs { get; init; } = MutuallyInclusiveRoleIDs;
    public IReadOnlyList<Snowflake> MutuallyExclusiveRoleIDs { get; init; } = MutuallyExclusiveRoleIDs;
}