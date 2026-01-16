using TickerQ.Utilities.Enums;
using TickerQ.Utilities.Interfaces;

namespace JobScraper.Web.Modules.Jobs;

public class JobsExceptionHandler : ITickerExceptionHandler
{
    private readonly ILogger<JobsExceptionHandler> _logger;

    public JobsExceptionHandler(ILogger<JobsExceptionHandler> logger) => _logger = logger;

    public Task HandleExceptionAsync(Exception exception, Guid tickerId, TickerType tickerType)
    {
        _logger.LogError(exception,
            "An error occurred while processing the job. Ticker ID: {TickerId}, Type: {TickerType}",
            tickerId,
            tickerType);

        return Task.CompletedTask;
    }


    public Task HandleCanceledExceptionAsync(Exception exception, Guid tickerId, TickerType tickerType)
    {
        _logger.LogError(exception, "The job was canceled. Ticker ID: {TickerId}, Type: {TickerType}", tickerId, tickerType);

        return Task.CompletedTask;
    }
}
