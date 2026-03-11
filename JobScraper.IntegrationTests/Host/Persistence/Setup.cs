using JobScraper.Web.Modules.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JobScraper.IntegrationTests.Host.Persistence;

public static class SetupPersistence
{
    /// <see cref="Setup.AddPersistence(WebApplicationBuilder)" />
    public static SqliteConnection AddTestPersistence(this IServiceCollection services,
        string connectionString)
    {
        // Keep a shared connection open so in-memory SQLite DB persists
        var keepAliveConnection = new SqliteConnection(connectionString);
        keepAliveConnection.Open();

        // Remove all existing DbContext-related registrations
        services.RemoveAll<IDbContextFactory<JobsDbContext>>();
        services.RemoveAll<JobsDbContext>();
        services.RemoveAll<DbContextOptions<JobsDbContext>>();
        services.RemoveAll<DbContextOptions>();

        // Register non-pooled factory (pooled factory can't resolve scoped services from root)
        services.AddDbContextFactory<JobsDbContext>((serviceProvider, options) =>
        {
            options.UseSqlite(keepAliveConnection);
            options.DefaultAppConfiguration(serviceProvider);

            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging();

            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        // Decorate with UserJobsContextFactory (same as production)
        services.Decorate<IDbContextFactory<JobsDbContext>, UserJobsContextFactory>();

        // Resolve DbContext from factory
        services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<JobsDbContext>>().CreateDbContext());

        return keepAliveConnection;
    }
}
