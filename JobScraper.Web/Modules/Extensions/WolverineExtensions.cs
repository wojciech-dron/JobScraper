using ErrorOr;
using Polly;
using Wolverine;

namespace JobScraper.Web.Modules.Extensions;

public static class WolverineExtensions
{
    public static async Task<ErrorOr<TResponse>> InvokeWithRetryAsync<TResponse>(this IMessageBus messageBus,
        object request,
        int retryAttempts = 1,
        CancellationToken cancellationToken = default)
        where TResponse : new()
    {
        var retryPolicy = Policy.Handle<Exception>()
            .WaitAndRetryAsync(retryAttempts, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        try
        {
            return await retryPolicy.ExecuteAsync(async () =>
                await messageBus.InvokeAsync<TResponse>(request, cancellationToken));
        }
        catch (Exception e)
        {
            return Error.Failure(description: $"Exception occurred while sending request. Message {e.Message}");
        }
    }
}
