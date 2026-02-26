using ErrorOr;
using Mediator;
using Polly;

namespace JobScraper.Web.Modules.Extensions;

public static class MediatrExtensions
{
    public static async Task<ErrorOr<TResponse>> SendWithRetry<TResponse>(this IMediator mediator,
        IRequest<TResponse> request,
        int retryAttempts = 1,
        CancellationToken cancellationToken = default)
        where TResponse : new()
    {
        var retryPolicy = Policy.Handle<Exception>()
            .WaitAndRetryAsync(retryAttempts, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        try
        {
            return await retryPolicy.ExecuteAsync(async () =>
                await mediator.Send(request, cancellationToken));
        }
        catch (Exception e)
        {
            return Error.Failure(description: $"Exception occurred while sending request. Message {e.Message}");
        }
    }
}
