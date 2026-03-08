using Serilog.Context;

namespace JobScraper.Web.Integration.DelegatingHandlers;

public sealed class LoggingDelegatingHandler(
    ILogger<LoggingDelegatingHandler> logger,
    IConfiguration configuration
) : DelegatingHandler
{
    private bool PayloadLoggingEnabled { get; } = configuration.GetValue<bool>("Integration:LogHttpPayloads");
    private int PayloadLimit { get; } = configuration.GetValue<int?>("Integration:LogHttpPayloadsLimit") ?? 4096;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        var logLevel = response.IsSuccessStatusCode ? LogLevel.Information : LogLevel.Warning;
        if (!logger.IsEnabled(logLevel))
            return response;

        var requestBody = await GetBodyOrNull(request.Content, cancellationToken);
        using var requestBodyScope = LogContext.PushProperty("RequestBody", requestBody);

        var responseBody = await GetBodyOrNull(response.Content, cancellationToken);
        using var responseBodyScope = LogContext.PushProperty("ResponseBody", responseBody);

        var method = response.RequestMessage?.Method.Method;
        var requestUrl = response.RequestMessage?.RequestUri?.AbsoluteUri;

        logger.Log(logLevel,
            "Received {Status} response from: {Method} {RequestUrl}",
            response.StatusCode,
            method,
            requestUrl);

        return response;
    }

    private async Task<string?> GetBodyOrNull(HttpContent? content, CancellationToken cancellationToken)
    {
        if (content is null)
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
}
