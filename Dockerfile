# =====================================================
# Multi-stage Dockerfile for .NET 8 + Pyroscope Profiler
# =====================================================

# =========================
# BUILD STAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj & restore
COPY src/*.csproj ./
RUN dotnet restore

# Copy source & build
COPY src/ ./
RUN dotnet publish -c Release -o /app/publish

# =========================
# RUNTIME STAGE
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Install Pyroscope Native Profiler (linux/amd64 only)
RUN apt-get update && apt-get install -y wget && \
    wget -q https://github.com/grafana/pyroscope-dotnet/releases/download/v0.8.14-pyroscope/pyroscope.0.8.14-glibc-x86_64.tar.gz && \
    tar -xzf pyroscope.0.8.14-glibc-x86_64.tar.gz -C /app && \
    rm pyroscope.0.8.14-glibc-x86_64.tar.gz && \
    apt-get purge -y wget && apt-get autoremove -y && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# =========================
# PYROSCOPE NATIVE PROFILER
# =========================
ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={BD1A650D-AC5D-4896-B64F-D6FA25D6B26A}
ENV CORECLR_PROFILER_PATH=/app/Pyroscope.Profiler.Native.so
ENV LD_PRELOAD=/app/Pyroscope.Linux.ApiWrapper.x64.so

# =========================
# APP DEFAULT CONFIG
# =========================
ENV ASPNETCORE_URLS=http://+:8080
ENV OTEL_SERVICE_NAME=dotnet-ecommerce
ENV OTEL_EXPORTER_OTLP_ENDPOINT=http://alloy.monitoring.svc.cluster.local:4318
ENV PYROSCOPE_APPLICATION_NAME=dotnet-ecommerce
ENV PYROSCOPE_SERVER_ADDRESS=http://pyroscope-distributor.monitoring.svc.cluster.local:4040
ENV PYROSCOPE_PROFILING_ENABLED=1
ENV PYROSCOPE_PROFILING_CPU_ENABLED=true
ENV PYROSCOPE_PROFILING_ALLOCATION_ENABLED=true
ENV DATABASE_DSN=Host=postgres.app.svc.cluster.local;Database=shop;Username=postgres;Password=postgres

EXPOSE 8080

ENTRYPOINT ["dotnet", "DotnetEcommerce.dll"]
