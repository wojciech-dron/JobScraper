using JobScraper.Web.Integration.AiProvider;
using Microsoft.Extensions.Options;

namespace JobScraper.Web.Integration;

public static class Setup
{
    public static WebApplicationBuilder AddIntegrationServices(this WebApplicationBuilder builder)
    {
        AddAiProviders(builder);

        return builder;
    }

    private static void AddAiProviders(WebApplicationBuilder builder)
    {
        builder.Services.Configure<AiProvidersConfig>(builder.Configuration.GetSection(AiProvidersConfig.SectionBase));

        var providers = builder.Services.BuildServiceProvider()
            .GetRequiredService<IOptions<AiProvidersConfig>>()
            .Value;

        foreach (var provider in providers)
            builder.AddAiProvider(provider.Key);
    }

}
