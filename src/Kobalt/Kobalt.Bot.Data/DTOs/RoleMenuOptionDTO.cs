using Remora.Rest.Core;

namespace Kobalt.Bot.Data.DTOs;

public class RoleMenuOptionDTO(int Id, string Name, string Description, Snowflake RoleID, IReadOnlyList<Snowflake> MutuallyInclusiveRoleIDs, IReadOnlyList<Snowflake> MutuallyExclusiveRoleIDs)
{
    public int Id { get; set; } = Id;
    public string Name { get; set; } = Name;
    public string Description { get; set; } = Description;
    public Snowflake RoleID { get; set; } = RoleID;
    public IReadOnlyList<Snowflake> MutuallyInclusiveRoleIDs { get; set; } = MutuallyInclusiveRoleIDs;
    public IReadOnlyList<Snowflake> MutuallyExclusiveRoleIDs { get; set; } = MutuallyExclusiveRoleIDs;
}