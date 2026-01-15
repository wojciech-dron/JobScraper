using JobScraper.Persistence.Interceptors;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JobScraper.Persistence;

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

            options
                .UseSqlite(connectionString)
                .AddInterceptors(new UpdatableInterceptor())
                .EnableSensitiveDataLogging();
        });

        services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<JobsDbContext>>().CreateDbContext());

        return builder;
    }

    public static async Task PrepareDbAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<JobsDbContext>();

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
