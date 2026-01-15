using Mediator;

namespace JobScraper.Web.Modules.Mediator;

public static class Setup
{
    /// <remarks> Must be here because of source generation </remarks>
    public static WebApplicationBuilder AddMediatorModule(this WebApplicationBuilder builder)
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
