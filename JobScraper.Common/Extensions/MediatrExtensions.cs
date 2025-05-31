using MediatR;
using Polly;

namespace JobScraper.Common.Extensions;

public static class MediatrExtensions
{
    public static Task SendWithRetry<T>(this IMediator mediator, T request, CancellationToken cancellationToken = default,
        int retryAttempts = 1)
        where T : IRequest
    {
        var retryPolicy = Policy.Handle<Exception>()
            .WaitAndRetryAsync(retryAttempts, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        try
        {
            return retryPolicy.ExecuteAsync(async () => await mediator.Send(request, cancellationToken));
        }
        catch (Exception e)
        {
            // fast workaround, dont stop when its an error
            Console.WriteLine(e);
            return Task.CompletedTask;
        }
    }
}