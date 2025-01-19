using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

namespace JobScraper.Utils;

public static class OpenTelemetry
{
    public static ILoggingBuilder AddOtelLogging(this ILoggingBuilder builder,
        IConfiguration configuration)
    {
        var otlpEndpoint = configuration["OtelEndpoint"];
        if (string.IsNullOrWhiteSpace(otlpEndpoint))
            return builder;

        const string serviceName = "JobScraper";

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