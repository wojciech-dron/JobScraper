using JobScraper.Models;

namespace JobScraper;

public static class Setup
{
    public static IServiceCollection AddScrapperServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AppSettings>(configuration.GetSection(AppSettings.SectionName));

        return services.AddMediatR(c => c.RegisterServicesFromAssemblyContaining(typeof(Setup)));
    }
}