namespace JobScraper;

public static class Setup
{
    public static IServiceCollection AddScrapperServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddMediatR(c => c.RegisterServicesFromAssemblyContaining(typeof(Setup)));
    }
}