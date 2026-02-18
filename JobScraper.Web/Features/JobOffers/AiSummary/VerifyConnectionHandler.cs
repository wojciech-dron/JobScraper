using ErrorOr;
using Mediator;

namespace JobScraper.Web.Features.JobOffers.AiSummary;

public class VerifyConnection
{
    public record Request(AiSummaryConfig Config) : IRequest<ErrorOr<Response>>;

    public record Response(string Data);

    internal class Handler : IRequestHandler<Request, ErrorOr<Response>>
    {
        public async ValueTask<ErrorOr<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var config = request.Config;

            var client = new HttpClient
            {
                BaseAddress = new Uri(config.BaseUrl),
            };

            var response = await client.GetAsync("api/v1/models", cancellationToken);

            if (!response.IsSuccessStatusCode)
                return Error.Unexpected(description: await response.Content.ReadAsStringAsync(cancellationToken));

            return new Response(await response.Content.ReadAsStringAsync(cancellationToken));
        }
    }
}
