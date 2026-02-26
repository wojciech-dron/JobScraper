using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace JobScraper.Web.Integration.AiProvider;

public static class AiKernel
{
    internal static WebApplicationBuilder AddAiProvider(this WebApplicationBuilder builder, string providerName)
    {
        var normalizedName = providerName.NormalizeAiProviderName();
        builder.Services.AddHttpClient(normalizedName,
            (sp, client) =>
            {
                var config = sp.GetRequiredService<IOptions<AiProvidersConfig>>().Value[normalizedName];
                ArgumentException.ThrowIfNullOrWhiteSpace(config.BaseUrl);
                client.BaseAddress = new Uri(config.BaseUrl);
            });

        builder.Services.AddKeyedScoped<Kernel>(normalizedName,
            (sp, _) =>
            {
                var config = sp.GetRequiredService<IOptions<AiProvidersConfig>>().Value[normalizedName];
                var clientFactory = sp.GetRequiredService<IHttpClientFactory>();

                ArgumentException.ThrowIfNullOrWhiteSpace(config.ApiKey);
                ArgumentException.ThrowIfNullOrWhiteSpace(config.ModelId);

                var client = clientFactory.CreateClient(normalizedName);

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

    public static Kernel GetAiKernel(this IServiceProvider sp, string providerName) =>
        sp.GetRequiredKeyedService<Kernel>(providerName.NormalizeAiProviderName());

    public static string NormalizeAiProviderName(this string providerName) =>
        providerName.ToUpperInvariant();
}
