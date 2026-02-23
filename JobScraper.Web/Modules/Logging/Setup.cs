using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Extensions;
using Serilog.Formatting.Display;
using Serilog.Sinks.OpenTelemetry;

namespace JobScraper.Web.Modules.Logging;

public static class Setup
{
    public static WebApplicationBuilder AddLogging(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var oltpEndpoint = configuration["OtelEndpoint"] ?? configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317";

        var protocolString = configuration["OtelProtocol"] ?? configuration["OTEL_EXPORTER_OTLP_PROTOCOL"];
        var protocol = Enum.TryParse(protocolString, true, out OtlpProtocol protocolEnum)
            ? protocolEnum
            : OtlpProtocol.Grpc;

        var serviceName = builder.Environment.ApplicationName;

        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithClientIp()
            .Enrich.WithProcessId()
            .Enrich.WithEnvironmentName()
            .Enrich.WithThreadId()
            .Enrich.WithRequestQuery()
            .ReadFrom.Configuration(builder.Configuration)
            .WriteTo.OpenTelemetry(o =>
            {
                o.Protocol = protocol;
                o.Endpoint = oltpEndpoint;

                if (protocol == OtlpProtocol.HttpProtobuf)
                {
                    o.LogsEndpoint = oltpEndpoint   + "/v1/logs";
                    o.TracesEndpoint = oltpEndpoint + "/v1/traces";
                }
                else
                {
                    o.TracesEndpoint = oltpEndpoint;
                    o.LogsEndpoint = oltpEndpoint;
                }

                o.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = serviceName,
                };
                o.OnBeginSuppressInstrumentation = SuppressInstrumentationScope.Begin;
                o.IncludedData = IncludedData.TraceIdField
                  | IncludedData.SpanIdField
                  | IncludedData.TemplateBody
                  | IncludedData.SpecRequiredResourceAttributes
                  | IncludedData.MessageTemplateTextAttribute
                  | IncludedData.SourceContextAttribute
                  | IncludedData.StructureValueTypeTags;
            })
            .WriteTo.Console(new MessageTemplateTextFormatter(
                "[{Timestamp:HH:mm:ss.fff} {Level:u3} {SourceContext:l} {UserName:l} {TraceId:l}] " +
                "{Message:lj} {NewLine}{Exception}"))
            .CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog();

        ConfigureOpenTelemetry(builder);

        return builder;
    }

    private static WebApplicationBuilder ConfigureOpenTelemetry(WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;

        var oltpEndpoint = configuration["OtelEndpoint"] ?? configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (string.IsNullOrWhiteSpace(oltpEndpoint))
            return builder;

        var protocolString = configuration["OtelProtocol"] ?? configuration["OTEL_EXPORTER_OTLP_PROTOCOL"];
        var protocol = Enum.TryParse(protocolString, true, out OtlpExportProtocol protocolEnum)
            ? protocolEnum
            : OtlpExportProtocol.Grpc;

        var metricsEndpoint = protocol == OtlpExportProtocol.HttpProtobuf
            ? oltpEndpoint + "/v1/metrics"
            : oltpEndpoint;

        var traceEndpoint = protocol == OtlpExportProtocol.HttpProtobuf
            ? oltpEndpoint + "/v1/traces"
            : oltpEndpoint;

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(c => c.AddService(builder.Environment.ApplicationName))
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddOtlpExporter(o =>
                    {
                        o.Protocol = protocol;
                        o.Endpoint = new Uri(metricsEndpoint);
                    });
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(builder.Environment.ApplicationName)
                    .AddHttpClientInstrumentation() // handling traceparent headers for System.Diagnostics.Activity
                    .AddAspNetCoreInstrumentation()
                    .AddOtlpExporter(o =>
                    {
                        o.Protocol = protocol;
                        o.Endpoint = new Uri(traceEndpoint);
                    });
            });

        return builder;
    }

    public static WebApplication UseAppLogging(this WebApplication app)
    {
        app.UseMiddleware<UserLogContextMiddleware>();

        return app;
    }

}
