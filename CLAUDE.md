# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

This is a .NET 8.0 shared library repository providing reusable infrastructure components for Azure-based applications. The library follows a modular architecture with separate packages for authentication, monitoring (OpenTelemetry), storage, caching, HTTP clients, and data access (CosmosDB, Kusto).

## Build Commands

### Basic Operations
```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run unit tests
dotnet test --filter Category=unit_test

# Build and pack NuGet packages
dotnet pack -c Release -o ./packages
```

### Using Makefile
```bash
make restore    # Restore dependencies
make build      # Build solution
make test       # Run unit tests
make pack       # Create NuGet packages
make clean      # Clean build artifacts
```

### Running Specific Tests
```bash
# Run tests in a specific project
dotnet test src/Common.Monitoring.Tests/Common.Monitoring.Tests.csproj

# Run tests with specific tag
dotnet test --filter Category=integration_test

# Run single test scenario (BDD/Reqnroll)
dotnet test --filter "FullyQualifiedName~ScenarioName"
```

## Architecture

### Project Structure
The repository contains 22 projects organized as:
- **Core Libraries**: Common.Shared, Common.Config, Common.Auth, Common.Monitoring, Common.Http, Common.Hosts
- **Data Access**: Common.DocDb (CosmosDB), Common.Kusto, Common.Storage (Azure Storage)
- **Infrastructure**: Common.Cache, Common.KeyVault, Common.XmlSchema
- **Business Logic**: Rule.Expressions (expression parser/evaluator)
- **Test Projects**: Corresponding .Tests projects using xUnit and Reqnroll (BDD)

### Service Registration Pattern
All major components use a consistent DI registration pattern with `IServiceCollection` extensions:

- `services.AddR9Auth(configuration)` - Authentication (AAD/MSI/SPN/User)
- `services.AddMonitoring(configuration)` - OpenTelemetry logging, metrics, and tracing
- `services.AddLogging(configuration)` - Logging sinks (Console, File, Geneva, OTLP, AppInsights)
- `services.AddMetrics(configuration)` - Metrics exporters (Prometheus, OTLP, Geneva)
- `services.AddTracing(configuration)` - Distributed tracing exporters (Zipkin, Jaeger, OTLP)
- `services.AddRestApiClient<TClient, TImpl, TAuthHandler>()` - HTTP clients with auth and policies

### Configuration System
Uses `IConfiguration.GetConfiguredSettings<T>()` to bind strongly-typed settings from appsettings.json. Configuration sections typically include:
- `AadSettings` - Azure AD authentication configuration
- `MonitorSettings` - OpenTelemetry sink configurations
- `VaultSettings` - Key Vault access settings
- Module-specific settings (KustoSettings, DocDbSettings, etc.)

### Authentication Architecture
The `Common.Auth` module supports multiple identity modes:
- **None**: No authentication
- **SPN**: Service Principal Name
- **MSI**: Managed Service Identity
- **User**: Interactive user authentication (with device code flow for local dev)

Different resources can use different identities by embedding separate `AadSettings` in their configuration sections.

### Monitoring Architecture
Built on OpenTelemetry with a unified builder pattern:
- **Logging**: Use `ILogger<T>` for traditional logging or static partial classes with extension methods for structured, high-performance logging
- **Tracing**: Inject `TraceProvider` to create `Tracer` instances, use `StartActiveSpan()` to instrument activities
- **Metrics**: Runtime and ASP.NET Core metrics auto-instrumented; inject `IMeter` for custom counters, gauges, histograms
- **Exporters**: Configurable sinks including Console, File, OTLP, Prometheus, Geneva, Zipkin, Jaeger, Application Insights

### HTTP Client Architecture
- Uses `IHttpClientFactory` with cached `HttpClientHandler` instances
- Supports resilience policies via Polly (retry, circuit breaker, timeout)
- Authentication via message handlers: `clientBuilder.AddHttpMessageHandler<TAuthHandler>()`
- Distributed tracing automatically instrumented via `Activity`

### Cache Architecture
Two-tier caching strategy:
- **Layer 1**: In-memory cache with LRU eviction, capacity limits, and file spillover
- **Layer 2**: Azure Blob Storage (chosen over Redis for large payloads 1-10 MB)
- **Invalidation**: Based on last modification time with overridable behavior per scenario

### Data Access
- **CosmosDB**: Strongly-typed wrapper around `CosmosClient` with CRUD and bulk import
- **Kusto**: Strongly-typed wrapper with automatic POCO-to-table schema mapping (cached in-memory)
- Both support authentication via connection string (from Key Vault) or MSI

## Testing

### Framework
Tests use **Reqnroll** (SpecFlow successor) for BDD-style tests:
- `.feature` files define scenarios in Gherkin syntax
- Step definitions in `Steps/*.cs` files
- Dependency injection scoped per scenario for parallelism
- xUnit as the test runner

### Test Categories
- `Category=unit_test` - Fast, isolated unit tests (run in CI)
- `Category=integration_test` - Tests requiring external resources

### Running Tests Locally
```bash
# Run all unit tests
dotnet test --filter Category=unit_test

# Run tests for specific module
dotnet test src/Common.Monitoring.Tests

# Run specific feature file
dotnet test --filter "FullyQualifiedName~TracesTestFeature"
```

## Code Standards

### Versioning
- Uses **Nerdbank.GitVersioning** for automatic semantic versioning
- Version defined in `version.json`: current base version is 0.5
- Build metadata automatically appended from git history

### Package Management
- Central package version management via `Packages.props`
- Key versions: .NET Extensions 9.0.3, OpenTelemetry 1.12.0, Kusto 13.0.2
- Custom NuGet feed configured in `NuGet.config`

### Code Style
- C# 10 language features enabled
- 4-space indentation for C# files
- File headers required: `// <copyright file="{fileName}" company="Microsoft Corp.">`
- Namespace-scoped using directives
- Assembly naming: `CRP.{ProjectName}`

### OpenTelemetry Version Notes
- **Important**: Version 1.12.0+ removed `grpc.core` and `google.grpc` due to security issues
- For OTLP/gRPC, follow migration guide: https://github.com/open-telemetry/opentelemetry-dotnet/issues/6209
- .NET Framework clients require version 1.11.1+

## GitHub Actions CI/CD
The build workflow (`.github/workflows/build.yml`):
- Runs on push/PR to `main` branch
- Matrix build: Debug/Release × ubuntu/windows/macos
- Tests run only on ubuntu-latest Debug builds
- Code coverage via Codecov
- NuGet packages published to GitHub Packages on Release/ubuntu builds
