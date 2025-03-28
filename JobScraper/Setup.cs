using JobScraper.Models;

namespace JobScraper;

public static class Setup
{
    public static IServiceCollection AddScrapperServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMediatR(c => c.RegisterServicesFromAssemblyContaining(typeof(Setup)));

        // services.Replace(new ServiceDescriptor(typeof(IRequestHandler<SyncJobsFromList.Command>), typeof(SyncJobsFromList.Handler),
        //     ServiceLifetime.Transient));

        services.Configure<ScraperConfig>(configuration.GetSection(ScraperConfig.SectionName));


        return services;
    }
}