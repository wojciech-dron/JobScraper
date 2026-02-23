using System.Net;
using Serilog.Context;

namespace JobScraper.Web.Integration.DelegatingHandlers;

public partial class LoggingDelegatingHandler(
    ILogger<LoggingDelegatingHandler> logger,
    IConfiguration configuration
) : DelegatingHandler
{
    private bool PayloadLoggingEnabled { get; } = configuration.GetValue<bool>("Integration:LogHttpPayloads");
    private int PayloadLimit { get; } = configuration.GetValue<int?>("Integration:LogHttpPayloadsLimit") ?? 4096;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var requestBody = await GetBodyOrNull(request.Content, cancellationToken);
        using var requestBodyScope = LogContext.PushProperty("RequestBody", requestBody);

        var requestUrl = request.RequestUri?.AbsoluteUri;
        var method = request.Method.Method;

        var response = await base.SendAsync(request, cancellationToken);

        var responseBody = await GetBodyOrNull(response.Content, cancellationToken);
        using var responseBodyScope = LogContext.PushProperty("ResponseBody", responseBody);
        LogResponse(response.StatusCode, method, requestUrl);

        return response;
    }

    private async Task<string?> GetBodyOrNull(HttpContent? content, CancellationToken cancellationToken)
    {
        if (content is null)
            return null;

        if (!logger.IsEnabled(LogLevel.Information))
            return null;

        if (!PayloadLoggingEnabled)
            return null;

        try
        {
            var body = await content.ReadAsStringAsync(cancellationToken);
            return body.Length > PayloadLimit ? body[..PayloadLimit] : body;
        }
        catch (Exception)
        {
            return null;
        }
    }

    [LoggerMessage(LogLevel.Information, "Received {status} response from: {method} {requestUrl}")]
    partial void LogResponse(HttpStatusCode status, string method, string? requestUrl);
}
