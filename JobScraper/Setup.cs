using JobScraper.Data;
using JobScraper.Models;
using JobScraper.Scrapers;
using Microsoft.EntityFrameworkCore;
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

        services.AddDbContext<JobDbContext>((services, options) =>
        {
            var connectionString = services
                .GetRequiredService<IConfiguration>()
                .GetConnectionString("DefaultConnection");

            options
                .UseSqlServer(connectionString!)
                .EnableSensitiveDataLogging();
        });

        services.Configure<ScraperConfig>(configuration.GetSection(ScraperConfig.SectionName));
        services.AddScoped<IndeedListScraper>();
        services.AddScoped<IndeedDetailsScraper>();

        return services;
    }

}