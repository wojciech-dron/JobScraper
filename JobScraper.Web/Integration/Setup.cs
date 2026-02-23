using JobScraper.Web.Integration.AiProvider;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Options;

namespace JobScraper.Web.Integration;

public static class Setup
{
    public static WebApplicationBuilder AddIntegrationServices(this WebApplicationBuilder builder)
    {
        builder.AddAiProviders();
        builder.AddLoggingForHttpClients();

        return builder;
    }

    private static void AddAiProviders(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<AiProvidersConfig>(builder.Configuration.GetSection(AiProvidersConfig.SectionBase));

        var providers = builder.Services.BuildServiceProvider()
            .GetRequiredService<IOptions<AiProvidersConfig>>()
            .Value;

        foreach (var provider in providers)
            builder.AddAiProvider(provider.Key);
    }

    private static WebApplicationBuilder AddLoggingForHttpClients(this WebApplicationBuilder builder)
    {
        var loggingFields = HttpLoggingFields.RequestMethod
          | HttpLoggingFields.RequestPath
          | HttpLoggingFields.ResponseStatusCode
          | HttpLoggingFields.Duration;

        if (builder.Configuration.GetValue<bool>("Integration:LogHttpPayloads"))
            loggingFields |= HttpLoggingFields.RequestBody | HttpLoggingFields.ResponseBody;

        builder.Services.AddHttpLogging(o =>
        {
            o.CombineLogs = true;
            o.RequestBodyLogLimit = 1024  * 16;
            o.ResponseBodyLogLimit = 1024 * 16;
            o.LoggingFields = loggingFields;
        });

        return builder;
    }


    public static WebApplication UseIntegrationServices(this WebApplication app)
    {
        app.UseHttpLogging();

        return app;
    }
}
