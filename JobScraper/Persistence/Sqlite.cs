using JobScraper.Persistance;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JobScraper.Persistence;

public static class Sqlite
{
    internal static IServiceCollection AddSqlitePersistance(this IServiceCollection services)
    {
        services.AddDbContext<JobsDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider
                .GetRequiredService<IConfiguration>()
                .GetConnectionString("DefaultConnection")!;

            options
                .UseSqlite(connectionString)
                .EnableSensitiveDataLogging();
        });

        return services;
    }

    internal static void PrepareDb(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<JobsDbContext>();

        EnsureDbDirectoryExists(dbContext);

        dbContext.Database.EnsureCreated();
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