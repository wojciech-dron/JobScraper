using JasperFx.Resources;
using JobScraper.Web.Modules.Persistence;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Sqlite;

namespace JobScraper.Web.Modules.Wolverine;

public static class Setup
{
    /// <remarks> Must be here because of source generation </remarks>
    public static WebApplicationBuilder AddWolverineFxModule(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        builder.Host.UseWolverine(opts =>
            {
                opts.PersistMessagesWithSqlite(connectionString);
                opts.UseEntityFrameworkCoreTransactions();
                opts.Policies.AutoApplyTransactions();

                opts.Policies.AddMiddleware<WolverineLoggingMiddleware>();
            },
            ExtensionDiscovery.ManualOnly);

        builder.Host.UseResourceSetupOnStartup();


        return builder;
    }
}
