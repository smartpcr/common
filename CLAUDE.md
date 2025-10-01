# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET 8.0 library repository containing reusable components for Azure-based applications. It provides cross-platform libraries (Windows, Linux, macOS) for authentication, monitoring, caching, storage, and other common infrastructure concerns.

**Main solution:** `common.sln`

## Build & Test Commands

```bash
# Restore dependencies
dotnet restore
# or
make restore

# Build the solution
dotnet build
# or
make build

# Run unit tests (tests are tagged with Category=unit_test)
dotnet test --filter Category=unit_test
# or
make test

# Build a specific configuration
dotnet build --configuration Release

# Package libraries for distribution
dotnet pack -c Release -o ./packages
# or
make pack

# Clean build artifacts
dotnet clean
# or
make clean
```

## Testing

- Tests use xUnit and Reqnroll (successor to SpecFlow) for BDD-style tests
- All test projects end with `.Tests` suffix
- Tests are filtered by category: `Category=unit_test`
- Test projects use dependency injection scoped to scenarios for parallelism
- FluentAssertions is used for test assertions

## Project Architecture

### Core Library Modules

The solution is organized as a monorepo with multiple library projects under `src/`:

1. **Common.Auth** - AAD authentication with support for SPN, MSI, and User identities
   - Entry point: `services.AddR9Auth()` in `AadAuthBuilder.cs`
   - Supports multiple identities for accessing resources in different tenants
   - Local development supports Visual Studio authentication and device code flow

2. **Common.Monitoring** - OpenTelemetry-based observability
   - Entry point: `services.AddMonitoring()` in `MonitorBuilder.cs`
   - Unified logging, metrics, and distributed tracing
   - Multiple sink support: Console, File, Geneva, ApplicationInsights, OTLP, Prometheus, Zipkin, Jaeger
   - Custom structured logging via static partial class extensions
   - Runtime metrics (GC, CPU, thread queue, contention) automatically instrumented
   - **Common.Monitoring.Tools** - CLI tool for converting OTLP trace files to Tempo format

3. **Common.Cache** - Two-tier caching strategy
   - Local in-memory cache (LRU algorithm with capacity limits and file spillover)
   - Distributed cache using Azure Blob Storage (chosen over Redis for large payloads 1-10MB)
   - Cache invalidation based on last modification time

4. **Common.Http** - Enhanced HTTP client management
   - Uses `IHttpClientFactory` for connection pooling
   - `services.AddRestApiClient<TClient, TImpl, TAuthHandler>()` for authenticated clients
   - Policy injection via configuration (retry, circuit breaker, etc.)
   - Automatic distributed tracing instrumentation

5. **Common.Storage** - Azure Storage abstractions

6. **Common.DocDb** - Strongly-typed CosmosDB wrapper
   - CRUD and bulk import operations
   - Supports auth key (from Key Vault) or MSI authentication

7. **Common.Kusto** - Strongly-typed Kusto/ADX client
   - Query and ingest operations
   - Automatic POCO to Kusto table schema mapping with in-memory caching

8. **Common.KeyVault** - Azure Key Vault access

9. **Common.Config** - Configuration management

10. **Common.Shared** - Shared utilities

11. **Common.XmlSchema** - XML schema validation

12. **Rule.Expressions** - Rule engine with expression evaluation

### Build Configuration

- **Target Framework:** .NET 8.0 (specified in `global.json` and `Directory.Build.props`)
- **Language Version:** C# 10
- **Runtime Identifiers:** win-x64, linux-x64, osx-x64
- **Assembly Naming:** All assemblies are prefixed with `CRP.` (e.g., `CRP.Common.Auth`)
- **Versioning:** Git-based versioning using Nerdbank.GitVersioning
- **Code Analysis:** StyleCop rules enabled via `stylecop.ruleset`
- **Nullable Reference Types:** Enabled
- **Implicit Usings:** Disabled

### CI/CD

- GitHub Actions workflow: `.github/workflows/build.yml`
- Cross-platform builds on Ubuntu, Windows, and macOS
- Tests run only on Ubuntu in Debug configuration
- Code coverage collected with Coverlet and published to Codecov
- NuGet packages published to GitHub Packages from Release builds on Ubuntu
- Requires `NUGET_AUTH_TOKEN` and `CODECOV_TOKEN` secrets

## Development Patterns

### Service Registration

All libraries follow the ASP.NET Core dependency injection pattern with extension methods on `IServiceCollection`:

```csharp
services.AddR9Auth(configuration);           // Authentication
services.AddMonitoring(configuration);        // Logging, Metrics, Tracing
services.AddHttpClient<TClient, TImpl>();     // HTTP clients
```

### Configuration

- Configuration is read via `configuration.GetConfiguredSettings<T>()` extension method
- Settings classes typically mirror the configuration section structure
- Key Vault integration for sensitive data (connection strings, auth keys)

### Monitoring Integration

- Structured logging: Create static partial classes with extension methods on `ILogger<T>`
- Distributed tracing: Use injected `TraceProvider` to create `Tracer`, then `StartActiveSpan()`
- Custom metrics: Use injected `IMeter` to create `Counter`, `Gauge`, or `Histogram`
