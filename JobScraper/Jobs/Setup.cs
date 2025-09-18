using JobScraper.Persistence;
using TickerQ.Dashboard.DependencyInjection;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.DependencyInjection;
using TickerQ.Utilities.Enums;

namespace JobScraper.Jobs;

public static class Setup
{
    public static IServiceCollection AddJobs(this IServiceCollection services,
        IConfiguration configuration)
    {
        // services.AddScoped<JobsExceptionHandler>();
        services.AddTickerQ(opt =>
        {
            opt.SetExceptionHandler<JobsExceptionHandler>();

            opt.AddOperationalStore<JobsDbContext>(efOpt =>
            {
                efOpt.UseModelCustomizerForMigrations();
                efOpt.CancelMissedTickersOnAppStart();
            });

            opt.AddDashboard(dbopt =>
            {
                dbopt.BasePath = "/tickerq-dashboard";
            });
        });

        return services;
    }

    public static IApplicationBuilder UseJobs(this IApplicationBuilder app)
    {
        app.UseTickerQ(qStartMode: TickerQStartMode.Immediate);

        return app;
    }
}