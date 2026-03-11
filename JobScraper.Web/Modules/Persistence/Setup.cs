using JobScraper.Web.Modules.Persistence.Interceptors;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace JobScraper.Web.Modules.Persistence;

public static class Setup
{
    public static WebApplicationBuilder AddPersistence(this WebApplicationBuilder builder)
    {
        var services = builder.Services;

        services.AddPooledDbContextFactory<JobsDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider
                .GetRequiredService<IConfiguration>()
                .GetConnectionString("DefaultConnection")!;

            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            options.UseSqlite(connectionString);
            options.DefaultAppConfiguration(serviceProvider);
        });

        // decorate IDbContextFactory with CurrentUserName resolution
        builder.Services.Decorate<IDbContextFactory<JobsDbContext>, UserJobsContextFactory>();

        // resolve db context with custom pooled factory
        services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<JobsDbContext>>().CreateDbContext());

        return builder;
    }

    internal static void DefaultAppConfiguration(this DbContextOptionsBuilder options,
        IServiceProvider serviceProvider)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        options
            .AddInterceptors(new UpdatableInterceptor(), new OwnerInterceptor())
            .UseProjectables()
            .UseLoggerFactory(loggerFactory)
            .EnableSensitiveDataLogging()
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    public static async Task PrepareDbAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<JobsDbContext>();

        var sqliteConn = dbContext.Database.GetDbConnection() as SqliteConnection;
        var csb = new SqliteConnectionStringBuilder(sqliteConn?.ConnectionString);
        var isInMemory = csb.Mode == SqliteOpenMode.Memory || csb.DataSource is "" or ":memory:";
        if (!isInMemory)
            EnsureDbDirectoryExists(dbContext);

        await dbContext.Database.MigrateAsync();
    }

    private static void EnsureDbDirectoryExists(JobsDbContext dbContext)
    {
        var connectionString = dbContext.Database.GetConnectionString();
        var connectionBuilder = new SqliteConnectionStringBuilder(connectionString);
        var sourceDirectory = Path.GetDirectoryName(connectionBuilder.DataSource);
        if (sourceDirectory is not null)
            Directory.CreateDirectory(sourceDirectory);
    }
}
