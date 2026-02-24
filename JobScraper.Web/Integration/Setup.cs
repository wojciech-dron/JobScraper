using JobScraper.Web.Integration.AiProvider;
using JobScraper.Web.Integration.DelegatingHandlers;
using Microsoft.Extensions.Options;

namespace JobScraper.Web.Integration;

public static class Setup
{
    public static WebApplicationBuilder AddIntegrationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<LoggingDelegatingHandler>();

        builder.Services.AddHttpClient()
            .ConfigureHttpClientDefaults(clientBuilder =>
            {
                clientBuilder.AddHttpMessageHandler<LoggingDelegatingHandler>();
            });

        builder.AddAiProviders();

        return builder;
    }

    private static void AddAiProviders(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<AiProvidersConfig>(builder.Configuration.GetSection(AiProvidersConfig.SectionBase));

        var providers = builder.Services.BuildServiceProvider()
            .GetRequiredService<IOptions<AiProvidersConfig>>()
            .Value;

        foreach (var provider in providers)
            builder.AddAiProvider(provider.Key.ToUpperInvariant());
    }


    public static WebApplication UseIntegrationServices(this WebApplication app) => app;
}
