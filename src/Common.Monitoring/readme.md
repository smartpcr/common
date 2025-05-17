

### Google.GRPC issues

grpc.core, google.grpc, etc are removed from version 1.12.0 and above. The following links are useful to understand the issues:

- [issue](https://github.com/open-telemetry/opentelemetry-dotnet/issues/4395)
- [commit to remove it](https://github.com/open-telemetry/opentelemetry-dotnet/commit/b9be07a27d4bb2384fa28a7d29dea9ebe3c32ca1)
- Grpc.Core deprecated due to security issues, to continue to use OLTP/grpc, follow instruction [here](https://github.com/open-telemetry/opentelemetry-dotnet/issues/6209)
- for net framework client, make sure using version 1.11.1 or higher, [bug](https://github.com/open-telemetry/opentelemetry-dotnet/issues/6067)
-