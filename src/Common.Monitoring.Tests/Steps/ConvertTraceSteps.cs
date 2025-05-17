// -----------------------------------------------------------------------
// <copyright file="TraceTestSteps.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Tests.Steps
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using Common.Monitoring.Utils;
    using FluentAssertions;
    using Reqnroll;

    [Binding]
    public class ConvertTraceSteps
    {
        private readonly ScenarioContext context;
        private readonly IReqnrollOutputHelper outputHelper;

        public ConvertTraceSteps(ScenarioContext context, IReqnrollOutputHelper outputHelper)
        {
            this.context = context;
            this.outputHelper = outputHelper;
        }

        [Given("otlp trace file at \"([^\"]+)\"")]
        public void GivenOtlpTraceFileAt(string otlpTraceFile)
        {
            this.context.Set(otlpTraceFile, "otlpTraceFile");
        }

        [When("I export the trace to a temp folder \"([^\"]+)\"")]
        public void WhenIExportTheTraceToATempFile(string tempTraceFolder)
        {
            var otlpTraceFile = this.context.Get<string>("otlpTraceFile");
            this.outputHelper.WriteLine($"reading otlp trace file {otlpTraceFile}...");
            var parser = new OtlpTraceParser();
            var parsedTempTraces = parser.TempoTraceFromOtlpJsonFile(otlpTraceFile);
            this.outputHelper.WriteLine($"parsed {parsedTempTraces.Count} traces from otlp trace file {otlpTraceFile}...");

            this.context.Set(tempTraceFolder, "tempTraceFolder");
            if (!Directory.Exists(tempTraceFolder))
            {
                Directory.CreateDirectory(tempTraceFolder);
            }

            foreach (var trace in parsedTempTraces)
            {
                var traceId = trace.traceId;
                var root = trace.root;
                var firstSpan = root.Batches
                    .SelectMany(b => b.InstrumentationLibrarySpans)
                    .SelectMany(s => s.Spans)
                    .OrderBy(s => s.StartTimeUnixNano)
                    .FirstOrDefault();
                if (firstSpan == null)
                {
                    continue;
                }

                var unixMilliseconds = firstSpan.StartTimeUnixNano / 1_000_000;
                var spanStartTime = DateTimeOffset.FromUnixTimeMilliseconds((long)unixMilliseconds).UtcDateTime;
                var traceFileName = $"{spanStartTime.ToLocalTime():yyyyMMdd-HHmm}-{firstSpan.Name}-{traceId.Substring(0, 6)}.json";
                var invalidChars = Path.GetInvalidFileNameChars();
                var sanitizedFileName = new string(traceFileName.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
                var tempTraceFile = Path.Combine(tempTraceFolder, sanitizedFileName);
                File.WriteAllText(tempTraceFile, JsonSerializer.Serialize(root, parser.Options));
            }
        }

        [Then("the temp files should exist")]
        public void ThenTheTempFileShouldExist()
        {
            var tempTraceFolder = this.context.Get<string>("tempTraceFolder");
            var tempTraceFiles = Directory.GetFiles(tempTraceFolder);
            tempTraceFiles.Should().NotBeEmpty();
        }
    }
}