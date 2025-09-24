using Microsoft.EntityFrameworkCore;
using redb.Core.SQLite.Migrations;

namespace redb.Core.SQLite
{
    public partial class RedbContext(DbContextOptions<RedbContext> options) : Core.RedbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            RedbContextModelSnapshot.RedbBuildModel(modelBuilder);
            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    }


    //public class RedbContextSQLiteFactory : IDesignTimeDbContextFactory<RedbContextSQLite>
    //{

    //    public RedbContextSQLite CreateDbContext(string[] args)
    //    {
    //        var Configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
    //        var optionsBuilder = new DbContextOptionsBuilder<RedbContextSQLite>();
    //        optionsBuilder.UseSqlite(Configuration["Data Source=redb.db3"]);

    //        return new RedbContextSQLite(optionsBuilder.Options);
    //    }
    //}
}
