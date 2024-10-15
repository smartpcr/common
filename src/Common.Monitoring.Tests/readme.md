
## Test tracing with Jaeger

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
   - 16686: Jaeger UI
   - 14268: Jaeger HTTP Thrift (used by OTLP)
   - 14250: Jaeger gRPC endpoint
   - 6831/6832: Jaeger UDP (standard tracing ports)
   - 9411: Zipkin-compatible endpoint

3. run tests

   - add Jaeger setting to sinks
     ```json
     {
       "Sinks": {
         "Jaeger": {
           "Host": "172.20.102.248",
           "Port": 6831
         }
       }
     }
     ```
   - add Jaeger sink types to tracing
     ```json
     {
       "MonitorSettings": {
         "Traces": {
           "SinkTypes": "Console, File, Jaeger"
         }
       }
     }
     ```

## Test with MLTP (Metrics, Logs, Traces and Profiles) stack

```bash
git clone git@github.com:grafana/intro-to-mltp.git
docker compose -f docker-compose.yml up
```