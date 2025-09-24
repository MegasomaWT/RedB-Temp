using Microsoft.EntityFrameworkCore;
using redb.Core.MSSql.Migrations;

namespace redb.Core.MSSql;
public partial class RedbContext(DbContextOptions<RedbContext> options) : Core.RedbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        RedbContextModelSnapshot.RedbBuildModel(modelBuilder);
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
