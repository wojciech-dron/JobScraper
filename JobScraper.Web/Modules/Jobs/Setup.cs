using JobScraper.Web.Modules.Persistence;
using TickerQ.Dashboard.DependencyInjection;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.Customizer;
using TickerQ.EntityFrameworkCore.DependencyInjection;

namespace JobScraper.Web.Modules.Jobs;

public static class Setup
{
    public static WebApplicationBuilder AddJobs(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration.GetSection(TickerConfig.SectionName).Get<TickerConfig>();

        builder.Services.AddTickerQ(options =>
        {
            options.SetExceptionHandler<JobsExceptionHandler>();
            options.ConfigureScheduler(scheduler =>
            {
                scheduler.MaxConcurrency = 10;                         // Maximum concurrent worker threads
                scheduler.NodeIdentifier = "production-server-01";     // Unique node identifier
                scheduler.IdleWorkerTimeOut = TimeSpan.FromMinutes(1); // Idle worker timeout
                scheduler.SchedulerTimeZone = TimeZoneInfo.Local;      // Timezone for scheduling
            });

            options.AddOperationalStore(efOptions =>
            {
                efOptions.UseApplicationDbContext<JobsDbContext>(ConfigurationType.UseModelCustomizer);
            });

            if (config?.Dashboard.Enabled == true)
                options.AddDashboard(dbopt =>
                {
                    dbopt.SetBasePath("/tickerq-dashboard");

                    if (config.Dashboard.UseBasicAuth)
                        dbopt.WithBasicAuth(config.Dashboard.User, config.Dashboard.Password);
                });
        });

        return builder;
    }

    public static IApplicationBuilder UseJobs(this IApplicationBuilder app)
    {
        app.UseTickerQ();
        return app;
    }
}

public class TickerConfig
{
    public const string SectionName = "AppSettings:TickerQ";
    public TickerDashboardConfig Dashboard { get; set; } = new();
}

public class TickerDashboardConfig
{
    public string? BasePath { get; set; }
    public string? User { get; set; }
    public string? Password { get; set; }

    public bool Enabled => !string.IsNullOrWhiteSpace(BasePath);
    public bool UseBasicAuth => !string.IsNullOrWhiteSpace(User) && !string.IsNullOrWhiteSpace(Password);
}
