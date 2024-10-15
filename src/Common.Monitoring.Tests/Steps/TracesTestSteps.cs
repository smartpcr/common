// -----------------------------------------------------------------------
// <copyright file="TracesTestSteps.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Tests.Steps
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using Common.Config;
    using Common.Monitoring.Tracing;
    using FluentAssertions;
    using Microsoft.Extensions.AmbientMetadata;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using OpenTelemetry.Trace;
    using TechTalk.SpecFlow;
    using TechTalk.SpecFlow.Infrastructure;

    [Binding]
    public class TracesTestSteps
    {
        private readonly ScenarioContext context;
        private readonly Tracer tracer;
        private readonly string tracerSourceName;
        private readonly DateTimeOffset testStartTime = DateTimeOffset.UtcNow;
        private readonly TelemetrySpan rootSpan;

        public TracesTestSteps(ScenarioContext context, ISpecFlowOutputHelper outputWriter)
        {
            this.context = context;

            var serviceProvider = this.context.Get<IServiceProvider>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            loggerFactory.CreateLogger<TracesTestSteps>();
            var traceProvider = serviceProvider.GetRequiredService<TracerProvider>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var metadata = configuration.GetConfiguredSettings<ApplicationMetadata>();
            this.tracerSourceName = $"{metadata.ApplicationName}.{nameof(TracesTestSteps)}";
            this.tracer = traceProvider.GetTracer(this.tracerSourceName, metadata.BuildVersion);

            // this force other created spans to be children of this span, so that we have same traceId
            this.rootSpan = this.tracer.StartActiveSpan(nameof(TracesTestSteps));
            this.context.Set(this.rootSpan, nameof(TracesTestSteps)); // this got disposed when the test ends
        }

        [Given(@"^a number (\d+)$")]
        public void GivenANumber(int number)
        {
            using var span = this.tracer.StartActiveSpan(nameof(this.GivenANumber));
            span.SetAttribute("input.number", number);
            this.context.Set(number, "number");
        }

        [When(@"^I calculate fibonacci of the number$")]
        public void WhenICalculateFibonacciOfTheNumber()
        {
            using var span = this.tracer.StartActiveSpan(nameof(this.WhenICalculateFibonacciOfTheNumber));
            var number = this.context.Get<int>("number");
            var result = this.Fibonacci(number);
            span.SetAttribute("result", result);
            this.context.Set(result, "result");
        }

        [Then(@"^the result should be (\d+)$")]
        public void ThenTheResultShouldBe(int expected)
        {
            using var span = this.tracer.StartActiveSpan(nameof(this.ThenTheResultShouldBe));
            span.SetAttribute("expected", expected);
            var result = this.context.Get<int>("result");
            span.SetAttribute("actual", result);
            result.Should().Be(expected);
        }

        [Then(@"^I should have the following traces$")]
        public void ThenIShouldHaveTheFollowingTraces(Table table)
        {
            this.rootSpan.Dispose(); // this will end the trace

            var logsFolder = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            var metricFiles = Directory.GetFiles(logsFolder, "trace_*.log", SearchOption.AllDirectories);
            metricFiles.Should().NotBeNullOrEmpty();
            var lastMetricFile = metricFiles.Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTime).First();
            var traceFileParser = new TraceFileParser(lastMetricFile.FullName);
            var spans = traceFileParser.Parse(this.tracerSourceName);
            spans.Should().NotBeNullOrEmpty();
            var lastTraceId = spans
                .Where(s => s.Timestamp >= this.testStartTime)
                .OrderByDescending(s => s.Timestamp).First().TraceId;
            spans = spans.Where(s => s.TraceId == lastTraceId).ToList();
            spans.Count.Should().BeGreaterOrEqualTo(table.Rows.Count);

            foreach (var row in table.Rows)
            {
                var opName = row["OperationName"];
                var foundSpans = spans.Where(s => s.OperationName == opName).ToList();
                foundSpans.Should().NotBeNullOrEmpty();
                var attributes = row["Attributes"];
                var kvpList = new List<KeyValuePair<string, string>>();
                if (!string.IsNullOrEmpty(attributes))
                {
                    var attributePairs = attributes.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var attributePair in attributePairs)
                    {
                        var pair = attributePair.Split(':');
                        if (pair.Length != 2)
                        {
                            continue;
                        }

                        var key = pair[0].Trim();
                        var value = pair[1].Trim();
                        kvpList.Add(new KeyValuePair<string, string>(key, value));
                    }
                }

                if (kvpList.Any())
                {
                    foundSpans = foundSpans.Where(s => kvpList.All(kvp =>
                        s.Attributes.ContainsKey(kvp.Key) &&
                        s.Attributes[kvp.Key] != null &&
                        s.Attributes[kvp.Key]!.ToString() == kvp.Value)).ToList();
                    foundSpans.Should().NotBeNullOrEmpty($"failed to find span with attributes {attributes} for {opName}");
                }

                var parentOpName = row["ParentOperationName"];
                if (!string.IsNullOrEmpty(parentOpName))
                {
                    var foundParent = spans.FirstOrDefault(s => foundSpans.Any(fs => fs.ParentId == s.Id));
                    foundParent.Should().NotBeNull($"failed to find parent span {parentOpName} for {opName} with attributes: {attributes}");
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private int Fibonacci(int n)
        {
            using var span = this.tracer.StartActiveSpan(nameof(this.Fibonacci));
            span.SetAttribute("input.n", n);

            if (n <= 1)
            {
                span.SetAttribute("result", n);
                return n;
            }

            var result = this.Fibonacci(n - 1) + this.Fibonacci(n - 2);
            span.SetAttribute("result", result);
            return result;
        }
    }
}