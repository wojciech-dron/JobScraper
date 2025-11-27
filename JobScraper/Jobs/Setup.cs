using JobScraper.Persistence;
using TickerQ.Dashboard.DependencyInjection;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.Customizer;
using TickerQ.EntityFrameworkCore.DependencyInjection;
using TickerQ.Utilities.Enums;

namespace JobScraper.Jobs;

public static class Setup
{
    public static IServiceCollection AddJobs(this IServiceCollection services,
        IConfiguration configuration)
    {
        // services.AddScoped<JobsExceptionHandler>();
        services.AddTickerQ(options =>
        {
            options.SetExceptionHandler<JobsExceptionHandler>();
            options.ConfigureScheduler(scheduler =>
            {
                scheduler.MaxConcurrency = 10;                         // Maximum concurrent worker threads
                scheduler.NodeIdentifier = "production-server-01";     // Unique node identifier
                scheduler.IdleWorkerTimeOut = TimeSpan.FromMinutes(1); // Idle worker timeout
                scheduler.SchedulerTimeZone = TimeZoneInfo.Utc;        // Timezone for scheduling
            });

            options.AddOperationalStore(efOptions =>
            {
                efOptions.UseApplicationDbContext<JobsDbContext>(ConfigurationType.UseModelCustomizer);
            });

            options.AddDashboard(dbopt =>
            {
                dbopt.SetBasePath("/tickerq-dashboard");
            });
        });

        return services;
    }

    public static IApplicationBuilder UseJobs(this IApplicationBuilder app)
    {
        app.UseTickerQ(TickerQStartMode.Immediate);
        return app;
    }
}
