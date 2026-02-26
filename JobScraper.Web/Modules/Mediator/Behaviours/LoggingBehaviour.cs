using System.Diagnostics;
using JobScraper.Web.Modules.Services;
using Mediator;
using Serilog.Context;

namespace JobScraper.Web.Modules.Mediator.Behaviours;

public partial class LoggingBehaviour<TRequest, TResponse>(
    ILogger<TRequest> logger,
    IUserProvider userProvider)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
{
    public async ValueTask<TResponse> Handle(TRequest message,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        var userName = userProvider.UserName ?? string.Empty;

        using var userNameScope = LogContext.PushProperty("UserName", userName);

        var beginTime = Stopwatch.GetTimestamp();

        var result = await next(message, cancellationToken);

        var elapsedMs = Stopwatch.GetElapsedTime(beginTime).TotalMilliseconds;
        var requestName = typeof(TRequest).FullName;

        LogRequest(logger, requestName, elapsedMs);

        return result;
    }

    [LoggerMessage(LogLevel.Information, "Mediator Request: {RequestName} finished in {ElapsedMs}ms")]
    static partial void LogRequest(ILogger<TRequest> logger, string? requestName, double elapsedMs);
}
