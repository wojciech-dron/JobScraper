using JobScraper.Jobs;
using JobScraper.Models;
using Mediator;

namespace JobScraper;

public static class Setup
{
    public static IServiceCollection AddScrapperServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AppSettings>(configuration.GetSection(AppSettings.SectionName));

        services.AddJobs(configuration);

        services.AddMediator((MediatorOptions options) =>
        {
            options.Namespace = "JobScraper.MediatorHandlers";
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.GenerateTypesAsInternal = true;
            options.NotificationPublisherType = typeof(ForeachAwaitPublisher);
            options.Assemblies = [typeof(Setup)];
            options.PipelineBehaviors = [];
            options.StreamPipelineBehaviors = [];
        });

        return services;
    }
}