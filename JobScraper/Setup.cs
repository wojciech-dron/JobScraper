using JobScraper.Logic.Common;
using JobScraper.Models;
using JobScraper.Scrapers.Indeed;
using JobScraper.Scrapers.JustJoinIt;
using MediatR;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JobScraper;

public static class Setup
{
    public static IServiceCollection AddScrapperServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMediatR(c => c.RegisterServicesFromAssemblyContaining(typeof(Setup)));

        services.Replace(new ServiceDescriptor(typeof(IRequestHandler<SyncJobsFromList.Command>), typeof(SyncJobsFromList.Handler),
            ServiceLifetime.Transient));

        services.AddScrapers(configuration);

        return services;
    }

    private static void AddScrapers(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ScraperConfig>(configuration.GetSection(ScraperConfig.SectionName));

        services.AddTransient<IndeedListScraper>();
        services.AddTransient<IndeedDetailsScraper>();

        services.AddTransient<JjitListScraper>();
        services.AddTransient<JjitDetailsScraper>();
    }
}