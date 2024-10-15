// -----------------------------------------------------------------------
// <copyright file="EtwEventSteps.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Tests.Steps
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using System.Threading.Tasks;
    using Common.Config;
    using Common.Monitoring.ETW;
    using Common.Monitoring.Tests.Hooks;
    using FluentAssertions;
    using Microsoft.Extensions.AmbientMetadata;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using OpenTelemetry;
    using OpenTelemetry.Trace;
    using TechTalk.SpecFlow;
    using TechTalk.SpecFlow.Assist;
    using TechTalk.SpecFlow.Infrastructure;

    [Binding]
    public class EtwEventSteps
    {
        private readonly ScenarioContext context;
        private readonly ISpecFlowOutputHelper outputWriter;
        private readonly Tracer tracer;
        private readonly string tracerSourceName;
        private readonly TelemetrySpan rootSpan;

        public EtwEventSteps(ScenarioContext context, ISpecFlowOutputHelper outputWriter)
        {
            this.context = context;
            this.outputWriter = outputWriter;

            var serviceProvider = this.context.Get<IServiceProvider>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            loggerFactory.CreateLogger<EtwEventSteps>();
            var traceProvider = serviceProvider.GetRequiredService<TracerProvider>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var metadata = configuration.GetConfiguredSettings<ApplicationMetadata>();
            this.tracerSourceName = $"{metadata.ApplicationName}.{nameof(EtwEventSteps)}";
            this.tracer = traceProvider.GetTracer(this.tracerSourceName, metadata.BuildVersion);

            // this force other created spans to be children of this span, so that we have same traceId
            this.rootSpan = this.tracer.StartActiveSpan(nameof(EtwEventSteps));
            this.context.Set(this.rootSpan, nameof(TracesTestSteps)); // this got disposed when the test ends
        }

        [Given(@"^the system have (\d+) services with the following methods and events$")]
        public void GivenServicesAreRunning(int serviceCount, Table table)
        {
            var eventSetups = table.CreateSet<ServiceEventSetup>()?.ToList() ?? new List<ServiceEventSetup>();
            eventSetups.Should().NotBeEmpty();
            var svcNames = eventSetups.Select(x => x.ServiceName).Distinct().ToList();
            svcNames.Should().HaveCount(serviceCount);
            var eventSources = new Dictionary<string, DynamicEventSource>();
            foreach (var svcName in svcNames)
            {
                eventSources[svcName] = new DynamicEventSource(svcName);
            }
            var traceListener = new TraceEventListener(this.context, this.outputWriter, eventSources.Keys.ToList());
            traceListener.Start();
            this.context.Set(traceListener);
        }

        [When(@"I call ""([^""]+)"" method of ""([^""]+)"" service to get list of products")]
        public async Task WHenICallMethodOfServiceToGetListOfProducts(string methodName, string serviceName)
        {
            var frontend = new FrontEnd(this.tracer);
            await frontend.HandleRequest();
        }

        [Then(@"the system should have collected the following events before timeout of (\d+) seconds")]
        public void ThenTheSystemShouldHaveCollectedTheFollowingEvents(int timeoutSeconds, Table table)
        {
            var expectedEvents = table.CreateSet<ExpectedEvent>()?.ToList() ?? new List<ExpectedEvent>();
            var lastEventToExpect = expectedEvents.OrderByDescending(evt => evt.Order).First();

            var stopWatch = Stopwatch.StartNew();
            var traceEvents = this.context.Get<List<TestTraceEvent>>();
            while (!traceEvents.Any(e => e.ProviderName == lastEventToExpect.ProviderName && e.EventName == lastEventToExpect.EventName) &&
                   stopWatch.Elapsed.TotalSeconds < timeoutSeconds)
            {
                Task.Delay(100).Wait();
                traceEvents = this.context.Get<List<TestTraceEvent>>();
            }

            traceEvents = traceEvents.Where(x => x.EventName != "ManifestData").ToList();
            traceEvents.Should().NotBeNullOrEmpty();

            foreach (var expectedEvent in expectedEvents)
            {
                var foundEvent = traceEvents.FirstOrDefault(x => x.ProviderName == expectedEvent.ProviderName && x.EventName == expectedEvent.EventName);
                foundEvent.Should().NotBeNull($"failed to find event on row {expectedEvent.Order}, with provider={expectedEvent.ProviderName} and event={expectedEvent.EventName}");

                if (expectedEvent.Tags?.Any() == true)
                {
                    foreach (var key in expectedEvent.Tags!.Keys)
                    {
                        foundEvent!.Payload.Should().ContainKey(key, $"failed to find tag {key} on row {expectedEvent.Order}, existing keys: {string.Join(", ", foundEvent.Payload.Keys)}");
                        foundEvent.Payload[key].Should().Be(expectedEvent.Tags[key].ToString(), $"failed to match tag value for {key} on row {expectedEvent.Order}, existing value: {foundEvent.Payload[key]}");
                    }
                }
            }
        }

        public class ServiceEventSetup
        {
            public string ServiceName { get; set; }
            public string MethodName { get; set; }
            public string EventName { get; set; }
            public int EventId { get; set; }
            public Dictionary<string, object>? Tags { get; set; }
            public int Latency { get; set; }
            public string? ChildCall { get; set; }
        }

        public class ExpectedEvent
        {
            public int Order { get; set; }
            public string ProviderName { get; set; }
            public string EventName { get; set; }
            public Dictionary<string, object>? Tags { get; set; }
        }

        public class Product
        {
            public string Name { get; set; }
            public string Category { get; set; }
            public decimal Price { get; set; }

            public static IEnumerable<Product> Generate(int count)
            {
                for (var i = 0; i < count; i++)
                {
                    yield return new Product
                    {
                        Name = $"Product-{i}",
                        Category = $"Category-{i}",
                        Price = i * 10
                    };
                }
            }
        }

        public class FrontEnd
        {
            private readonly Tracer tracer;

            public FrontEnd(Tracer tracer)
            {
                this.tracer = tracer;
            }

            public async Task HandleRequest()
            {
                using var span = this.tracer.StartActiveSpan(nameof(this.HandleRequest));
                await Task.Delay(1);
                FrontEndTestEventSource.Log.RequestReceived("/products");

                await Task.Delay(4);
                var backend = new BackEnd(this.tracer);
                FrontEndTestEventSource.Log.GetProducts("/products");
                var products = await backend.GetProducts();

                FrontEndTestEventSource.Log.RequestCompleted("/products", products.Count, "OK");
            }
        }

        public class BackEnd
        {
            private readonly Tracer tracer;

            public BackEnd(Tracer tracer)
            {
                this.tracer = tracer;
            }

            public async Task<List<Product>> GetProducts()
            {
                using var span = this.tracer.StartActiveSpan(nameof(this.GetProducts));
                await Task.Delay(10);
                BackEndTestEventSource.Log.GetProducts();

                BackEndTestEventSource.Log.BeginGetProductsFromCache("/products");
                var cache = new Cache(this.tracer);
                var (list, expiry) = cache.TryGet<List<Product>>("/products");
                BackEndTestEventSource.Log.FinishedGetProductsFromCache("/products", list != null && expiry >= DateTimeOffset.UtcNow);
                if (list != null && expiry > DateTimeOffset.UtcNow)
                {
                    return list;
                }

                var sql = "SELECT * FROM Products";
                BackEndTestEventSource.Log.BeginGetProductsFromDatabase(sql);
                var db = new Database(this.tracer);
                var products = await db.Query("SELECT * FROM Products");
                BackEndTestEventSource.Log.FinishedGetProductsFromDatabase(sql, products.Count);

                BackEndTestEventSource.Log.UpdateCache("/products", products.Count);
                await cache.Set("/products", products, DateTimeOffset.UtcNow.AddMinutes(5));

                BackEndTestEventSource.Log.FinishedGetProducts(products.Count);

                return products;
            }
        }

        public class Cache
        {
            private Dictionary<string, (object, DateTimeOffset)> cacheStore = new Dictionary<string, (object, DateTimeOffset)>();
            private readonly Tracer tracer;

            public Cache(Tracer tracer)
            {
                this.tracer = tracer;
            }

            public (T? value, DateTimeOffset expiry) TryGet<T>(string key)
            {
                using var span = this.tracer.StartActiveSpan(nameof(this.TryGet));

                if (this.cacheStore.TryGetValue(key, out var value))
                {
                    CacheTestEventSource.Log.CacheHit(key, 15);
                    return ((T)value.Item1, value.Item2);
                }

                CacheTestEventSource.Log.CacheMiss(key);
                return (default, DateTimeOffset.MinValue);
            }

            public async Task Set<T>(string key, T value, DateTimeOffset expiry)
            {
                using var span = this.tracer.StartActiveSpan(nameof(this.Set));

                await Task.Delay(20);
                this.cacheStore[key] = (value, expiry);
            }
        }

        public class Database
        {
            private readonly Tracer tracer;

            public Database(Tracer tracer)
            {
                this.tracer = tracer;
            }

            public async Task<List<Product>> Query(string sql)
            {
                using var span = this.tracer.StartActiveSpan(nameof(this.Query));

                await Task.Delay(50);
                DatabaseTestEventSource.Log.BeginQuery(sql);
                await Task.Delay(300);
                DatabaseTestEventSource.Log.FinishedQuery(sql, 15);
                return Product.Generate(15).ToList();
            }
        }

        [EventSource(Name = "FrontEnd")]
        public class FrontEndTestEventSource : EventSource
        {
            public static FrontEndTestEventSource Log = new FrontEndTestEventSource();

            [Event(1, Level = EventLevel.Informational, Message = "Request received")]
            public void RequestReceived(string path)
            {
                this.WriteEvent(1, path);
            }

            [Event(2, Level = EventLevel.Informational, Message = "Getting products from backend")]
            public void GetProducts(string path)
            {
                this.WriteEvent(2, path);
            }

            [Event(3, Level = EventLevel.Informational, Message = "Request completed")]
            public void RequestCompleted(string path, int count, string status_code)
            {
                this.WriteEvent(3, path, count, status_code);
            }
        }

        [EventSource(Name = "BackEnd")]
        public class BackEndTestEventSource : EventSource
        {
            public static BackEndTestEventSource Log = new BackEndTestEventSource();

            [Event(1, Level = EventLevel.Informational, Message = "Get products started")]
            public void GetProducts()
            {
                this.WriteEvent(1);
            }

            [Event(2, Level = EventLevel.Informational, Message = "Get products from cache")]
            public void BeginGetProductsFromCache(string cache_key)
            {
                this.WriteEvent(2, cache_key);
            }

            [Event(3, Level = EventLevel.Informational, Message = "Finished getting products from cache")]
            public void FinishedGetProductsFromCache(string cache_key, bool found)
            {
                this.WriteEvent(3, cache_key, found);
            }

            [Event(4, Level = EventLevel.Informational, Message = "Get products from database")]
            public void BeginGetProductsFromDatabase(string sql)
            {
                this.WriteEvent(4, sql);
            }

            [Event(5, Level = EventLevel.Informational, Message = "Get products from database")]
            public void FinishedGetProductsFromDatabase(string sql, int count)
            {
                this.WriteEvent(5, sql, count);
            }

            [Event(6, Level = EventLevel.Informational, Message = "Update cache")]
            public void UpdateCache(string cache_key, int count)
            {
                this.WriteEvent(6, cache_key, count);
            }

            [Event(7, Level = EventLevel.Informational, Message = "Finished getting products")]
            public void FinishedGetProducts(int count)
            {
                this.WriteEvent(7, count);
            }
        }

        [EventSource(Name = "Cache")]
        public class CacheTestEventSource : EventSource
        {
            public static CacheTestEventSource Log = new CacheTestEventSource();

            [Event(1, Level = EventLevel.Informational, Message = "Cache hit")]
            public void CacheHit(string cache_key, int count)
            {
                this.WriteEvent(1, cache_key, count);
            }

            [Event(2, Level = EventLevel.Informational, Message = "Cache miss")]
            public void CacheMiss(string cache_key)
            {
                this.WriteEvent(2, cache_key);
            }
        }

        [EventSource(Name = "Database")]
        public class DatabaseTestEventSource : EventSource
        {
            public static DatabaseTestEventSource Log = new DatabaseTestEventSource();

            [Event(1, Level = EventLevel.Informational, Message = "Query started")]
            public void BeginQuery(string sql)
            {
                this.WriteEvent(1, sql);
            }

            [Event(2, Level = EventLevel.Informational, Message = "Query completed")]
            public void FinishedQuery(string sql, int count)
            {
                this.WriteEvent(2, sql, count);
            }
        }
    }
}