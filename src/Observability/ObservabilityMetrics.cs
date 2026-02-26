using Prometheus;

namespace DotnetEcommerce.Observability;

/// <summary>
/// Prometheus metrics definitions.
/// Equivalent to Go metrics.
/// </summary>
public static class ObservabilityMetrics
{
    public static readonly Counter HttpRequestsTotal = Metrics
        .CreateCounter(
            "http_requests_total",
            "Total HTTP requests",
            new CounterConfiguration
            {
                LabelNames = new[] { "method", "endpoint", "status" }
            });

    public static readonly Histogram HttpRequestDuration = Metrics
        .CreateHistogram(
            "http_request_duration_seconds",
            "HTTP request duration in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "method", "endpoint" }
            });

    public static readonly Counter OrdersCreatedTotal = Metrics
        .CreateCounter(
            "orders_created_total",
            "Total orders created");
}
