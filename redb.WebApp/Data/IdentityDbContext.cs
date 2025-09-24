using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Configuration;

namespace redb.WebApp.Data
{
    public class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext(options)
    {

        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        //    optionsBuilder.UseSqlite(new ConfigurationBuilder().Build().GetConnectionString("IdentitySQLiteConnection"));
    }

    //public class IdentityContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
    //{
    //    public IdentityDbContext CreateDbContext(string[] args)
    //    {
    //        var Configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
    //        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
    //        optionsBuilder.UseSqlite(Configuration["Data:IdentitySQLiteConnection:ConnectionString"]);

    //        return new IdentityDbContext(optionsBuilder.Options);
    //    }
    //}
}
