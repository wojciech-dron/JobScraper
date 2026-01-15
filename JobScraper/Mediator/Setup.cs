using Mediator;

namespace JobScraper.Mediator;

public static class Setup
{
    public static WebApplicationBuilder AddMediator(this WebApplicationBuilder builder)
    {
        builder.Services.AddMediator(options =>
        {
            options.Namespace = "JobScraper.MediatorHandlers";
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.GenerateTypesAsInternal = true;
            options.NotificationPublisherType = typeof(ForeachAwaitPublisher);
            options.Assemblies = [typeof(Setup)];
            options.PipelineBehaviors = [];
            options.StreamPipelineBehaviors = [];
        });

        return builder;
    }
}
