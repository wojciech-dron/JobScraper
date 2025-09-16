using Mediator;
using Polly;

namespace JobScraper.Common.Extensions;

public static class MediatrExtensions
{
    public static async Task<TResponse> SendWithRetry<TResponse>(this IMediator mediator,
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default,
        int retryAttempts = 1)
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
            // fast workaround, dont stop when its an error
            Console.WriteLine(e);
            return new TResponse();
        }
    }
}