using ErrorOr;
using Mediator;

namespace JobScraper.Web.Features.JobOffers.AiSummary;

public class GetAvailableModels
{
    public record Request(AiSummaryConfig Config) : IRequest<ErrorOr<Response>>;

    public record Response(ModelsDto[] Models);

    internal class Handler(IHttpClientFactory clientFactory) : IRequestHandler<Request, ErrorOr<Response>>
    {
        public async ValueTask<ErrorOr<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var config = request.Config;

            var client = clientFactory.CreateClient();
            client.BaseAddress = new Uri(config.BaseUrl);

            var response = await client.GetAsync("api/v1/models", cancellationToken);

            if (!response.IsSuccessStatusCode)
                return Error.Unexpected(description: $"Server returned error code: {response.StatusCode}");

            var responseContent = await response.Content.ReadFromJsonAsync<ResponseDto>(cancellationToken);

            if (responseContent is null)
                return Error.Unexpected(description: "Failed to deserialize response");

            return new Response(responseContent.Data);
        }
    }

    public record ResponseDto(ModelsDto[] Data);

    public record ModelsDto(string Id, int Created);
}
