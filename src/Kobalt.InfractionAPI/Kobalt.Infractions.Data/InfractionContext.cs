using Kobalt.Infractions.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kobalt.Infractions.Data;

public class InfractionContext : DbContext
{
    public DbSet<Infraction> Infractions { get; set; }
    public DbSet<InfractionRule> InfractionRules { get; set; }
}
