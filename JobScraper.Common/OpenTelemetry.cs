using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

namespace JobScraper.Common;

public static class OpenTelemetry
{
    public static ILoggingBuilder AddOtelLogging(this ILoggingBuilder builder,
        IConfiguration configuration, string serviceName)
    {
        var otlpEndpoint = configuration["OtelEndpoint"];
        if (string.IsNullOrWhiteSpace(otlpEndpoint))
            return builder;

        builder.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;

            logging.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
            logging.AddOtlpExporter(opt => opt.Endpoint = new Uri(otlpEndpoint));
        });

        return builder;
    }
}