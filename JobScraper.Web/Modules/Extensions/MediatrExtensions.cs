using Mediator;
using Polly;

namespace JobScraper.Web.Modules.Extensions;

public static class MediatrExtensions
{
    public static async Task<TResponse> SendWithRetry<TResponse>(this IMediator mediator,
        IRequest<TResponse> request,
        ILogger? logger = null,
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
            if (logger is not null)
                logger.LogError(e, "Error occurred while sending request: {Request}", request);
            else
                Console.WriteLine($"Error occurred while sending request: {request}");

            return new TResponse();
        }
    }
}
