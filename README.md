# 🔵 .NET E-Commerce - Fully Observable Application

Minimal but fully observable ASP.NET Core 8 application for learning end-to-end observability with LGTM-P stack.

## 🎯 Purpose

Demonstrate complete observability in .NET:
- **Tracing**: HTTP → Database queries (OpenTelemetry)
- **Metrics**: Prometheus-compatible metrics
- **Logging**: Structured logs with trace correlation
- **Profiling**: Pyroscope Native Profiler (CPU + Allocations)

## 📁 Project Structure

```
dotnet-ecommerce-observable/
├── src/
│   ├── Program.cs                          # Entry point
│   ├── Controllers/
│   │   ├── HomeController.cs               # Clean business logic
│   │   └── CheckoutController.cs           # Clean business logic
│   ├── Repository/
│   │   ├── DbInitializer.cs                # Database setup
│   │   ├── ProductRepository.cs            # Clean data access
│   │   └── OrderRepository.cs              # Clean data access
│   ├── Observability/
│   │   ├── ObservabilityBootstrap.cs       # SINGLE ENTRY POINT
│   │   └── ObservabilityMetrics.cs         # Prometheus metrics
│   ├── Models/
│   │   └── Models.cs                       # Product, Order
│   └── Views/
│       ├── Index.cshtml                    # Product list
│       └── Success.cshtml                  # Order success
├── Dockerfile                              # Multi-stage with Pyroscope
├── k8s/
│   └── deployment.yaml                     # K8s manifests
└── README.md
```

## 🔍 Observability Architecture

### 1. Single Entry Point

**All observability is initialized in ONE place:**

```csharp
// Program.cs
ObservabilityBootstrap.Initialize(builder);
```

**Business logic (Controllers, Repository) does NOT depend on observability code.**

### 2. Tracing (OpenTelemetry → Tempo)

#### How It Works

```
HTTP Request
  ↓ [ASP.NET Core Instrumentation]
Controller
  ↓
Repository
  ↓ [Npgsql.OpenTelemetry]
PostgreSQL Query ← TRACED AS CHILD SPAN
  ↓
OTLP HTTP → Alloy → Tempo
```

#### Key Components

- **ASP.NET Core Instrumentation**: Auto-traces HTTP requests
- **Npgsql.OpenTelemetry**: Auto-traces PostgreSQL queries
- **OTLP Exporter**: Sends traces to Tempo via Alloy

#### Configuration

```csharp
services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddNpgsql()  // ← PostgreSQL query tracing
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(otlpEndpoint);
            options.Protocol = OtlpExportProtocol.HttpProtobuf;
        }));
```

**Result**: Every SQL query appears as a child span in Tempo traces.

### 3. Metrics (Prometheus)

#### Exposed Metrics

```
http_requests_total{method, endpoint, status}
http_request_duration_seconds{method, endpoint}
orders_created_total
```

#### Endpoint

```
GET /metrics
```

#### How It Works

- Uses `prometheus-net` library
- Metrics defined in `ObservabilityMetrics.cs`
- Automatically scraped by Prometheus/Alloy

### 4. Logging (Structured)

#### Configuration

```csharp
builder.Logging.AddJsonConsole();
```

#### Features

- Structured JSON logs to stdout
- Automatic trace correlation (trace_id, span_id)
- Collected by Loki via Alloy

### 5. Profiling (Pyroscope Native Profiler)

#### Why Native Profiler?

**SDK-style profiling** (like pyroscope-dotnet SDK) has limitations:
- Manual instrumentation required
- Limited profiling types
- Performance overhead

**Native Profiler** (CLR Profiling API):
- ✅ Zero code changes
- ✅ CPU profiling
- ✅ Allocation profiling
- ✅ Heap profiling (optional)
- ✅ Production-ready

#### How It Works

The Pyroscope Native Profiler uses the **CLR Profiling API**:

1. **CORECLR_ENABLE_PROFILING=1**: Enables CLR profiling
2. **CORECLR_PROFILER**: GUID of the profiler
3. **CORECLR_PROFILER_PATH**: Path to native profiler library
4. **LD_PRELOAD**: Preload API wrapper

```bash
# Environment variables
CORECLR_ENABLE_PROFILING=1
CORECLR_PROFILER={BD1A650D-AC5D-4896-B64F-D6FA25D6B26A}
CORECLR_PROFILER_PATH=/app/Pyroscope.Profiler.Native.so
LD_PRELOAD=/app/Pyroscope.Linux.ApiWrapper.x64.so
```

#### Profiling Types

- **CPU**: Samples CPU usage
- **Allocations**: Tracks memory allocations
- **Heap**: In-use memory (optional)

#### Installation in Dockerfile

```dockerfile
RUN wget -q https://github.com/grafana/pyroscope-dotnet/releases/download/v0.8.14-pyroscope/pyroscope.0.8.14-glibc-x86_64.tar.gz && \
    tar -xzf pyroscope.0.8.14-glibc-x86_64.tar.gz -C /app
```

## 🚀 Build & Deploy

### Local Build

```bash
cd src
dotnet restore
dotnet build
dotnet run
```

### Docker Build

```bash
docker build -t dotnet-ecommerce:latest .
```

**Important**: Build for `linux/amd64`:

```bash
docker buildx build --platform linux/amd64 -t dotnet-ecommerce:latest .
```

### Deploy to Kubernetes

```bash
kubectl apply -f k8s/deployment.yaml
```

## 🔧 Environment Variables

### Required

| Variable | Purpose | Example |
|----------|---------|---------|
| `DATABASE_DSN` | PostgreSQL connection | `Host=postgres.app.svc.cluster.local;Database=shop;Username=postgres;Password=postgres` |
| `OTEL_SERVICE_NAME` | Service name for tracing | `dotnet-ecommerce` |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | OTLP endpoint | `http://alloy.monitoring.svc.cluster.local:4318` |

### Pyroscope Native Profiler

| Variable | Purpose | Value |
|----------|---------|-------|
| `CORECLR_ENABLE_PROFILING` | Enable CLR profiling | `1` |
| `CORECLR_PROFILER` | Profiler GUID | `{BD1A650D-AC5D-4896-B64F-D6FA25D6B26A}` |
| `CORECLR_PROFILER_PATH` | Native profiler path | `/app/Pyroscope.Profiler.Native.so` |
| `LD_PRELOAD` | API wrapper | `/app/Pyroscope.Linux.ApiWrapper.x64.so` |
| `PYROSCOPE_APPLICATION_NAME` | App name in Pyroscope | `dotnet-ecommerce` |
| `PYROSCOPE_SERVER_ADDRESS` | Pyroscope endpoint | `http://pyroscope-distributor.monitoring.svc.cluster.local:4040` |
| `PYROSCOPE_PROFILING_ENABLED` | Enable profiling | `1` |
| `PYROSCOPE_PROFILING_CPU_ENABLED` | Enable CPU profiling | `true` |
| `PYROSCOPE_PROFILING_ALLOCATION_ENABLED` | Enable allocation profiling | `true` |

## 📊 Verification

### 1. Tracing (Tempo)

```bash
# Access Grafana
# Navigate to Explore → Tempo
# Search for service: dotnet-ecommerce
# Verify trace structure:

GET /
  └── HomeController.Index
      └── ProductRepository.GetAllAsync
          └── SELECT id, name, price FROM products  ← DB SPAN
```

### 2. Metrics (Prometheus)

```bash
# Access metrics endpoint
curl http://dotnet-ecommerce/metrics

# Verify metrics:
http_requests_total{method="GET",endpoint="/",status="200"} 15
http_request_duration_seconds_sum{method="GET",endpoint="/"} 0.45
orders_created_total 5
```

### 3. Logs (Loki)

```bash
# Access Grafana
# Navigate to Explore → Loki
# Query: {app="dotnet-ecommerce"}
# Verify structured logs with trace_id
```

### 4. Profiling (Pyroscope)

```bash
# Access Pyroscope UI
# Select application: dotnet-ecommerce
# Verify profiles:
# - CPU profile shows SimulateCpuWork()
# - Allocation profile shows memory allocations
```

## 🎓 Key Learnings

### 1. Clean Architecture

- **Observability**: Isolated in `/Observability`
- **Business Logic**: Controllers and Repository are clean
- **Single Entry Point**: `ObservabilityBootstrap.Initialize()`

### 2. Automatic Instrumentation

- **HTTP**: ASP.NET Core instrumentation
- **Database**: Npgsql.OpenTelemetry
- **No manual span creation** in business logic

### 3. Native Profiler vs SDK

| Aspect | SDK | Native Profiler |
|--------|-----|-----------------|
| Code Changes | Required | None |
| Profiling Types | Limited | Full (CPU, Alloc, Heap) |
| Performance | Overhead | Minimal |
| Production | Not recommended | Production-ready |

### 4. Trace Propagation

OpenTelemetry automatically propagates trace context:
- HTTP request creates root span
- Database queries create child spans
- All correlated by trace_id

## 🔗 References

- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [Npgsql OpenTelemetry](https://www.npgsql.org/doc/diagnostics/tracing.html)
- [Pyroscope .NET Native Profiler](https://grafana.com/docs/pyroscope/latest/configure-client/language-sdks/dotnet/)
- [prometheus-net](https://github.com/prometheus-net/prometheus-net)

---

**This application demonstrates production-grade observability in .NET with minimal code!** 🚀
