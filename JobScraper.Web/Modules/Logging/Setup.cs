using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Extensions;
using Serilog.Formatting.Display;

namespace JobScraper.Web.Modules.Logging;

public static class Setup
{
    public static WebApplicationBuilder AddLogging(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithClientIp()
            .Enrich.WithProcessId()
            .Enrich.WithRequestQuery()
            .WriteTo.OpenTelemetry()
            .WriteTo.Console(new MessageTemplateTextFormatter(
                "[{Timestamp:HH:mm:ss.fff} {Level:u3} {SourceContext:l} {TraceId:l}] {Message:lj} {NewLine}{Exception}"))
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog();

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
                    .AddHttpClientInstrumentation(); // handling traceparent headers for System.Diagnostics.Activity
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        var configuration = builder.Configuration;

        var oltpEndpoint = configuration["OtelEndpoint"] ?? configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

        if (string.IsNullOrWhiteSpace(oltpEndpoint))
            return builder;

        var protocolString = configuration["OtelProtocol"] ?? configuration["OTEL_EXPORTER_OTLP_PROTOCOL"];
        var protocol = Enum.TryParse(protocolString, true, out OtlpExportProtocol protocolEnum)
            ? protocolEnum
            : OtlpExportProtocol.Grpc;

        builder.Services.AddOpenTelemetry()
            .UseOtlpExporter(protocol, new Uri(oltpEndpoint));

        return builder;
    }
}
