using System.Diagnostics;
using JobScraper.Web.Modules.Services;
using Serilog.Context;
using Wolverine;

namespace JobScraper.Web.Modules.Wolverine;

public partial class WolverineLoggingMiddleware(
    IUserProvider userProvider)
{
    private IDisposable? userNameScope;
    private long beginTime;

    public void Before()
    {
        var userName = userProvider.UserName ?? string.Empty;
        userNameScope = LogContext.PushProperty("UserName", userName);
        beginTime = Stopwatch.GetTimestamp();
    }

    public void After(ILogger<WolverineLoggingMiddleware> logger, Envelope envelope)
    {
        var elapsedMs = Stopwatch.GetElapsedTime(beginTime).Milliseconds;
        var requestType = envelope.MessageType;
        LogRequest(logger, envelope.Id, requestType, elapsedMs);
    }

    public void Finally()
    {
        userNameScope?.Dispose();
        userNameScope = null;
    }

    [LoggerMessage(LogLevel.Information, "Wolverine Request: {RequestName} with Id {EnvelopeId} finished in {ElapsedMs}ms")]
    static partial void LogRequest(ILogger<WolverineLoggingMiddleware> logger, Guid envelopeId, string? requestName, int elapsedMs);
}
