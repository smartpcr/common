
## Enable otel

### Use otel for logs, metrics and traces

1. add nuget packages
- OpenTelemetry
- OpenTelemetry.Exporter.Console
- OpenTelemetry.Exporter.Jaeger
- OpenTelemetry.Exporter.Otlp
- OpenTelemetry.Instrumentation.Http
- OpenTelemetry.Extensions.Hosting

2. register otel providers

```cs
// add meter/metrics
var meterProviderBuilder = Sdk.CreateMeterProviderBuilder()
    .ConfigureResource(r => r.AddService(
        OtelSettings.ServiceName,
        serviceVersion: OtelSettings.ServiceVersion,
        serviceInstanceId: Environment.MachineName))
    .AddMeter(OtelSettings.ServiceName);

// configure metric exporter
meterProviderBuilder.AddConsoleExporter();

// register MeterProvider as singleton
container.RegisterInstance(typeof(MeterProvider), meterProviderBuilder.Build());

// add tracing
var traceProviderBuilder = Sdk.CreateTracerProviderBuilder()
    .AddSource(OtelSettings.ServiceName)
    .AddSource($"{OtelSettings.ServiceName}.*")
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(
        serviceName: OtelSettings.ServiceName,
        serviceVersion: OtelSettings.ServiceVersion,
        serviceInstanceId: Environment.MachineName))
    .AddHttpClientInstrumentation()
    .SetSampler(_ => CreateSampler(samplerType));

// configure trace exporter
traceProviderBuilder.AddConsoleExporter();

// register TracerProvider as singleton
var builder = traceProviderBuilder.Build();
Container.RegisterInstance<TracerProvider>(builder);
```

3. instrument code

```cs
using var tracer = tracerProvider.GetTracer("ServiceName");
using span = tracer.StartActiveSpan("MethodName");
try
{
	// code
}
catch (Exception ex)
{
    span.SetStatus(Status.Error);
    span.RecordException(ex);
    throw;
}
```

### Use Jaeger

1. start jaeger docker container

```bash
docker run -d --name jaeger \
    -e COLLECTOR_ZIPKIN_HTTP_PORT=9411 \
    -p 5775:5775/udp \
    -p 6831:6831/udp \
    -p 6832:6832/udp \
    -p 5778:5778 \
    -p 16686:16686 \
    -p 14268:14268 \
    -p 14250:14250 \
    -p 9411:9411 \
    jaegertracing/all-in-one:1.41
```

2. ports explanation:

- 5775/udp: This port is used for receiving traces in the Thrift compact protocol over UDP.
- 6831/udp: This port is used for receiving traces in the Thrift binary protocol over UDP.
- 6832/udp: This port is used for receiving traces in the Thrift compact protocol over UDP.
- 5778: This port is used for the agent's HTTP server, which provides a sampling strategy endpoint and a health check endpoint.
- 16686: This port is used for the Jaeger Query service, which provides the web UI for querying and visualizing traces.
- 14268: This port is used for the Jaeger Collector's HTTP endpoint for receiving spans directly from clients.
- 14250: This port is used for the Jaeger Collector's gRPC endpoint for receiving spans directly from clients.
- 9411: This port is used for the Zipkin-compatible endpoint, allowing clients that use Zipkin's instrumentation libraries to send traces to Jaeger.

3. run tests

- note ip address where container is running: `ip addr show eth0`
- add Jaeger (unfortunately, code change is necessary)
```cs
private static void SetupOtel(FeatureContext featureContext, Dictionary<string, string> configDict)
{
    configDict.Add(OtelSettings.OtelEnabledParameter, "true");
    configDict.Add(OtelSettings.SinkTypesParameter, "Console,File,Jaeger");
    configDict.Add(OtelSettings.JaegerAgentHostParameter, "172.20.102.248"); // jaeger container
    configDict.Add(OtelSettings.JaegerAgentPortParameter, "6831"); // jaeger port, should test 14250
    // ommit the rest of the code
}
```
- here is how jaeger exporter is configured in `UnityConfig` for URP and `OpenTelemetryUtils` for mocked services

```cs
traceProviderBuilder.AddJaegerExporter(opt =>
{
    opt.AgentHost = jaegerHost;
    opt.AgentPort = jaegerPort;
    opt.ExportProcessorType = ExportProcessorType.Simple; // Batch in prod
    opt.MaxPayloadSizeInBytes = 512;
});
```

### Test with MLTP (Metrics, Logs, Traces and Profiles) using grafana stack

1. start grafana stack
```bash
docker compose -f ./Setup/docker-compose.yml up -d
```
2. config:
```cs
// setup in ClassHook for test
private static void SetupOtel(FeatureContext featureContext, Dictionary<string, string> configDict)
{
    configDict.Add(OtelSettings.OtelEnabledParameter, "true");
    configDict.Add(OtelSettings.SinkTypesParameter, "Console,File,Jaeger");
    configDict.Add(OtelSettings.OtelEndpointParameter, "http://172.20.102.248:4317"); // alloy container
    // ommit the rest of the code
}

// setup in UnityConfig for URP and OpenTelemetryUtils for mocked services
traceProviderBuilder.AddOtlpExporter(options =>
{
    options.Endpoint = new Uri(otlpEndpoint);
    options.Protocol = OtlpExportProtocol.Grpc; // 4317, or use 4318 for HTTP/Protobuf
    options.ExportProcessorType = ExportProcessorType.Simple; // Batch in prod
    options.TimeoutMilliseconds = 1000;
});
```

