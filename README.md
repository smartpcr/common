### Authentication

#### Features

- Use `AadSettings` and `services.AddR9Auth()` to configure type of identity the app is running as, it can be None, SPN, MSI or User.
- Default identity identity is used to access protected resources (such as storage accounts, cosmos db, key vault, external api, etc). However, if external resource resides in different tenants, a separate identity can be used by embedding different `AadSettings` in configuration section.
- For local development, key vault can be configured to use current user, you could authenticate yourself from visual studio, or trigger device code authentication.

#### Usage

- [Example](./Common.Auth/readme.md)

### Monitoring

#### Features

- By adding `services.AddR9Monitoring()` in code, we support logging, metrics and tracing via __OpenTelmetry__.
- One or more sinks can be added, this is done in configuration.
    - Supported sinks for logging: Console, File, Geneva, ApplicationInsights, OTLP, etc.
    - Supported sinks for metrics: Console, File, OTLP, Prometheus, Geneva, ApplicationInsights, etc.
    - Supported sinks for tracing: Console, File, OTLP, Geneva, Zipkin, Jaeger, ApplicationInsights, etc.
- You are responsible for setting up the sink, for example, to use ApplicationInsights, you need to create application insights on Azure, and put instrumentation key in config file.
- For logging, you can use traditional way to directly write messages using ILogger<T>, or use a static partial class to add extension methods and write structured message. The later approach has better performance.
- For tracing, you can use injected `TraceProvider` to create a new `Tracer`, which is used to instrument Activity via `StartActiveSpan()`.
- For metrics, runtime and aspnetcore are instrumented to measure metrics such as GC time, CPU usage, thread queue size, contention, request count, request error rate, request latency, etc. In addition, you can use injected `IMeter` to create customer metrics (`Counter`, `Gauge` or `Histogram`).

#### Usage

- [Example](./Common.Monitoring/readme.md)
- service map
  ![service map](./docs/service-map.png)
- distributed tracing: api -> get all subscriptions -> get VMs for subscriptions in parallel, error when app running outside of IL17 hub and lost vpn access.
  ![distributed tracing](./docs/tracing.png)
- metrics
  ![metrics](./docs/metrics.png)
- logs: found a bug cronjob is unable to parse multiple schedules
  ![logs](./docs/logs.png)

### Http Client

#### Features

- HttpClient is added using `services.AddHttpClient<TContract, TImplementation>()` to inject `IHttpClientFactory` into `IServiceProvider`. `IHttpClientFactory` caches the HttpClientHandler instances created by the factory to reduce resource consumption. In addition, `IHttpClientFactory` can take advantage of resilient and transient-fault-handling third-party middleware with ease.
- When client requires bearer token, use `services.AddRestApiClient<TClient, TImpalementation, TAuthHandler>()`, which uses `clientBuilder.AddHttpMessageHandler<TAuthHandler>()` to add authentication handler to the pipeline.
- Client policy can be injected into pipeline via configuration. For example, to add retry policy, use `clientBuilder.AddPolicyHandlerFromConfig("RetryPolicy")`.
- Tracing is instrumented via `Activity` and `StartActiveSpan()`.

#### Usage

- [Example](./Common.HttpClient/readme.md)

### Storage

### Cache

#### Features

- implemented as two layers of cache: local in-memory for fast access, and distributed cache for shared access.
- in-memory cache uses last-recently-used algorithm, has capacity boundary and can spill over to file storage.
- we pick blob storage over redis for distributed cache, since payload size is big (1-10 MB).
- cache invalidation is triggered by last modification time. Each time before a cached item is accessed, a backend call is made to grab last write time and invalidate cache if it is older. However, the behavior can be overridden based on specific scenarios.

#### Usage

- [Example](./Common.Cache/readme.md)

### CosmosDB

#### Features

- a strongly-typed wrapper around `CosmosClient` to provide CRUD and bulk import operations.
- accessible by either authkey (stored in keyvault) or MSI

#### Usage

- [Example](./Common.DocDB/readme.md)

### Kusto

#### Features

- a strongly-typed wrapper around `KustoClient` to provide query and ingest operations.
- automatically map POCO to Kusto table schema, mapping and conversion is cached in-memory for each type to improve performance.
- TODO: support binary bulk ingest where schema is inferred from ETW.

#### Usage

- [Example](./Common.Kusto/readme.md)

### Specflow

Specflow is used to write BDD tests, it is a tool to turn plain text into executable tests, so that non-technical stakeholders can understand the behavior of the system.

#### Features

- dependency injection is scoped to scenario to improve test parallelism.
- support livingdoc that provides transparency and traceability of requirements, tests and their results.

#### Usage

- [Example](./Common.Config.Tests/readme.md)
