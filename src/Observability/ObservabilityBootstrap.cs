using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;

namespace DotnetEcommerce.Observability;

/// <summary>
/// Single entry point for all observability initialization.
/// Business logic does NOT depend on this.
/// </summary>
public static class ObservabilityBootstrap
{
    public static void Initialize(WebApplicationBuilder builder)
    {
        Console.WriteLine("🔍 Initializing observability...");

        // 1. OpenTelemetry Tracing
        InitializeTracing(builder.Services);

        // 2. Prometheus Metrics
        InitializeMetrics();

        // 3. Structured Logging (built-in)
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddJsonConsole();

        Console.WriteLine("✅ Observability initialized");
        Console.WriteLine("📊 Metrics: /metrics");
        Console.WriteLine("🔭 Tracing: OTLP HTTP");
        Console.WriteLine("🔥 Profiling: Pyroscope Native Profiler (via env vars)");
    }

    private static void InitializeTracing(IServiceCollection services)
    {
        var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "dotnet-ecommerce";
        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") 
            ?? "http://alloy.monitoring.svc.cluster.local:4318";

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddNpgsql() // PostgreSQL query tracing
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                    options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                })
                .SetSampler(new AlwaysOnSampler()));

        Console.WriteLine($"✅ Tracing → {otlpEndpoint}");
    }

    private static void InitializeMetrics()
    {
        // Prometheus metrics are initialized via prometheus-net
        // Custom metrics defined in ObservabilityMetrics.cs
        Console.WriteLine("✅ Metrics initialized");
    }
}
