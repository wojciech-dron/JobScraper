using Polly;
using Wolverine;

namespace JobScraper.Web.Modules.Extensions;

public static class WolverineExtensions
{
    public static async Task<TResponse> InvokeWithRetryAsync<TResponse>(this IMessageBus mediator,
        object request,
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
                await mediator.InvokeAsync<TResponse>(request, cancellationToken));
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
