using System.Net.Http.Headers;
using ErrorOr;
using JobScraper.Web.Integration.AiProvider;
using Mediator;
using Microsoft.Extensions.Options;

namespace JobScraper.Web.Features.AiSummary;

/// <remarks> Tested with openrouter only </remarks>
public class VerifyProviderAndGetModels
{
    public record Request(string ProviderName) : IRequest<ErrorOr<Response>>;

    public record Response(ModelsDto[] Models);

    internal class Handler(
        IHttpClientFactory clientFactory,
        IOptions<AiProvidersConfig> config
    ) : IRequestHandler<Request, ErrorOr<Response>>
    {
        public async ValueTask<ErrorOr<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var providerName = request.ProviderName;
            if (!config.Value.TryGetValue(providerName, out var settings))
                return Error.Unexpected(description: $"Provider '{providerName}' not found in configuration");

            var httpClient = clientFactory.CreateClient(providerName);

            var response = await httpClient.GetAsync("models", cancellationToken);

            if (!response.IsSuccessStatusCode)
                return Error.Unexpected(description: $"Server returned error code: {response.StatusCode}");

            var responseContent = await response.Content.ReadFromJsonAsync<ResponseDto>(cancellationToken);

            if (responseContent is null)
                return Error.Unexpected(description: "Failed to deserialize response");

            var models = responseContent.Data;
            var currentModelId = settings.ModelId;

            if (models.All(m => m.Id != currentModelId))
                return Error.Unexpected(description: $"Model '{currentModelId}' not found in available models");

            // verify api key
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
            var verifyKeyResponse = await httpClient.GetAsync("key", cancellationToken);
            if (!verifyKeyResponse.IsSuccessStatusCode)
                return Error.Unexpected(description: $"Api key for {providerName} is invalid.");

            return new Response(responseContent.Data);
        }
    }

    public record ResponseDto(ModelsDto[] Data);

    public record ModelsDto(string Id, int Created);
}
