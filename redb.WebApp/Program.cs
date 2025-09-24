using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using redb.Core;
using redb.Core.Utils;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace redb.WebApp
{
    public class Program
    {
        static Type[] tExt = [typeof(SqlServerDbContextOptionsExtensions),
                              typeof(NpgsqlDbContextOptionsBuilderExtensions),
                              typeof(SqliteDbContextOptionsBuilderExtensions)];
        public static async Task Main(string[] args)
        {
            String? DB_TYPE = Environment.GetEnvironmentVariable("DB_TYPE") ?? "UseSqlite";

            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("ru-Ru");

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            String IdentityConnectionString = builder.Configuration.GetConnectionString($"Identity{DB_TYPE}Connection") ?? throw new InvalidOperationException("Connection string Identity Connection not found.");
            String RedbConnectionString = builder.Configuration.GetConnectionString($"Redb{DB_TYPE}Connection") ?? throw new InvalidOperationException("Connection string Redb Connection not found.");
            switch (DB_TYPE)
            {
                //case "UseSqlite":
                //    SelectDBInstance<Core.SQLite.RedbContext, Core.SQLite.RedbService>()
                //    .AddDbContext<IdentityDbContext>(options => options.UseSqlite(IdentityConnectionString));
                //    break;
                //case "UseSqlServer":
                //    SelectDBInstance<Core.MSSql.RedbContext, Core.MSSql.RedbService>()
                //    .AddDbContext<IdentityDbContext>(options => options.UseSqlServer(IdentityConnectionString));
                //    break;
                case "UseNpgsql":
                    SelectDBInstance<Core.Postgres.RedbContext, Core.Postgres.RedbService>()
                    .AddDbContext<IdentityDbContext>(options => options.UseNpgsql(IdentityConnectionString));
                    break;
            }

            builder.Services
               .AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
               .AddEntityFrameworkStores<IdentityDbContext>();

            builder.Services.AddMvc();
            builder.Services.AddRazorPages(options => options.Conventions.AuthorizeFolder("/pageItems"));

            builder.Services.Configure<ForwardedHeadersOptions>(options => options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

            WebApplication app = builder.Build();

            app.Use((context, next) =>
            {
                context.Request.Scheme = "https";
                return next(context);
            }).UseForwardedHeaders();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection()
               .UseStaticFiles()
               .UseRouting()
               .UseAuthorization()
               .UseEndpoints(endpoints =>
               {
                   endpoints.MapGet("/Identity/Account/Register", context => Task.Factory.StartNew(() => context.Response.Redirect("/Identity/Account/Login", true, true)));
                   endpoints.MapPost("/Identity/Account/Register", context => Task.Factory.StartNew(() => context.Response.Redirect("/Identity/Account/Login", true, true)));
               });


            app.MapControllers();
            app.MapRazorPages();

            app.Run();

            IServiceCollection SelectDBInstance<T, V>() where T : RedbContext where V : class, IRedbService =>
            builder.Services
               .AddScoped<RedbContext, T>()
               .AddScoped<IRedbService, V>()
               .AddDbContext<T>(options =>
               {
                   MethodInfo? mi = null;
                   tExt.forEach<Type>(t =>
                   {
                       if (t.GetMethods().Any(n => n.Name == DB_TYPE))
                       {
                           foreach (var m in t.GetMethods())
                           {
                               var p = m.GetParameters();
                               if (p.Length == 3 && p[0].ParameterType == typeof(DbContextOptionsBuilder) && p[1].Name == "connectionString")
                               {
                                   mi = m;
                                   return;
                               }
                           }
                       }
                   });

                   (mi ?? throw new ArgumentException(DB_TYPE)).Invoke(options, [options, RedbConnectionString, null]);

                   options.UseLazyLoadingProxies();
                   options.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
               });
        }
    }
}

