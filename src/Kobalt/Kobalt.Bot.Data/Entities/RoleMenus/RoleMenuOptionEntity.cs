using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Remora.Rest.Core;

namespace Kobalt.Bot.Data.Entities.RoleMenus;

public class RoleMenuOptionEntity
{
    public int Id { get; set; }
    public int RoleMenuId { get; set; }

    public string Name { get; set; }
    public string Description { get; set; }

    [NotNull]
    public RoleMenuEntity? RoleMenu { get; set; }

    public Snowflake RoleID { get; set; }

    [NotMapped]
    public List<Snowflake> MutuallyInclusiveRoles { get; set; }

    [NotMapped]
    public List<Snowflake> MutuallyExclusiveRoles { get; set; }
}

public class RoleMenuOptionConfiguration : IEntityTypeConfiguration<RoleMenuOptionEntity>
{
    public void Configure(EntityTypeBuilder<RoleMenuOptionEntity> builder)
    {
        builder.ToTable("role_menu_options");

        // TODO: Update to EF Core 8, where primitive collections are supported natively (translated to JSONB)
        builder
        .Property(rmo => rmo.MutuallyInclusiveRoles)
        .HasConversion<string>
        (
            roles => string.Join(',', roles),
            str => str.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => new Snowflake(ulong.Parse(s), 0)).ToList()
        );

        builder
        .Property(rmo => rmo.MutuallyExclusiveRoles)
        .HasConversion<string>
        (
            roles => string.Join(',', roles),
            str => str.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => new Snowflake(ulong.Parse(s), 0)).ToList()
        );
    }
}
