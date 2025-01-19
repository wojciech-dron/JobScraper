using JobScraper.Logic;
using JobScraper.Models;
using JobScraper.Scrapers;
using JobScraper.Scrapers.JustJoinIt;
using MediatR;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Playwright;

namespace JobScraper;

public static class Setup
{
    public static async Task<IServiceCollection> AddScrapperServicesAsync(this IServiceCollection services,
        IConfiguration configuration)
    {
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Firefox.LaunchAsync();
        services.AddSingleton(browser);

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

        services.AddScoped<IndeedListScraper>();
        services.AddScoped<IndeedDetailsScraper>();

        services.AddScoped<JjitListScraper>();
    }
}