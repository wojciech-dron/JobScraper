using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace JobScraper.Web.Integration.AiProvider;

public static class AiKernel
{
    internal static WebApplicationBuilder AddAiProvider(this WebApplicationBuilder builder, string providerName)
    {
        builder.Services.AddHttpClient(providerName,
            (sp, client) =>
            {
                var config = sp.GetRequiredService<IOptions<AiProvidersConfig>>().Value[providerName];
                ArgumentException.ThrowIfNullOrWhiteSpace(config.BaseUrl);
                client.BaseAddress = new Uri(config.BaseUrl);
            });

        builder.Services.AddKeyedScoped<Kernel>(providerName,
            (sp, _) =>
            {
                var config = sp.GetRequiredService<IOptions<AiProvidersConfig>>().Value[providerName];
                var clientFactory = sp.GetRequiredService<IHttpClientFactory>();

                ArgumentException.ThrowIfNullOrWhiteSpace(config.ApiKey);
                ArgumentException.ThrowIfNullOrWhiteSpace(config.ModelId);

                var client = clientFactory.CreateClient(providerName);

                var kernel = Kernel.CreateBuilder()
                    .AddOpenAIChatClient(
                        config.ModelId,
                        config.ApiKey,
                        httpClient: client)
                    .Build();

                return kernel;
            });

        return builder;
    }
}
