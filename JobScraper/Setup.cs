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

        services.AddDbContext<JobsDbContext>((services, options) =>
        {
            var connectionString = services
                .GetRequiredService<IConfiguration>()
                .GetConnectionString("DefaultConnection");

            options
                .UseSqlite(connectionString)
                .EnableSensitiveDataLogging();
        });

        services.AddMediatR(c => c.RegisterServicesFromAssemblyContaining(typeof(Setup)));

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

    public static void PrepareDb(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<JobsDbContext>();
        dbContext.Database.EnsureCreated();
    }
}