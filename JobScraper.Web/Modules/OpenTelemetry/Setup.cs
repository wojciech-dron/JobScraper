using NReco.Logging.File;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace JobScraper.Web.Modules.OpenTelemetry;

public static class Setup
{
    public static WebApplicationBuilder AddLogging(this WebApplicationBuilder builder)
    {
        builder.Logging.AddFile(builder.Configuration.GetSection("Logging:File"));
        builder.ConfigureOpenTelemetry();

        return builder;
    }

    private static WebApplicationBuilder ConfigureOpenTelemetry(this WebApplicationBuilder builder)
    {
        var loggingBuilder = builder.Logging;
        var configuration = builder.Configuration;

        var otlpEndpoint = configuration["OtelEndpoint"]   ?? configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        var protocolString = configuration["OtelProtocol"] ?? configuration["OTEL_EXPORTER_OTLP_PROTOCOL"];
        var protocol = Enum.TryParse(protocolString, true, out OtlpExportProtocol protocolEnum)
            ? protocolEnum
            : OtlpExportProtocol.Grpc;

        if (string.IsNullOrWhiteSpace(otlpEndpoint))
            return builder;

        loggingBuilder.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(c => c.AddService(builder.Environment.ApplicationName))
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
            })
            .UseOtlpExporter(protocol, new Uri(otlpEndpoint));

        return builder;
    }
}
