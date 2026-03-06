using System.Diagnostics;
using Wolverine;

namespace JobScraper.Web.Modules.Wolverine.Middlewares;

public partial class WolverineLoggingMiddleware
{
    private long beginTime;

    public void Before() => beginTime = Stopwatch.GetTimestamp();
    public void After(ILogger<WolverineLoggingMiddleware> logger, Envelope envelope)
    {
        var elapsedMs = Stopwatch.GetElapsedTime(beginTime).TotalMilliseconds;
        var requestType = envelope.MessageType;
        LogRequest(logger, envelope.Id, requestType, elapsedMs);
    }

    [LoggerMessage(LogLevel.Information, "Wolverine Request: {RequestName} with Id {EnvelopeId} finished in {ElapsedMs}ms")]
    static partial void LogRequest(ILogger<WolverineLoggingMiddleware> logger,
        Guid envelopeId,
        string? requestName,
        double elapsedMs);
}
