// -----------------------------------------------------------------------
// <copyright file="TempoTraceModels.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Tools
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using CommandLine;
    using Common.Monitoring.Utils;

    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var options = new CmdOptions();
            Parser.Default.ParseArguments<CmdOptions>(args)
                .WithParsed(opts =>
                {
                    Console.WriteLine($"InputFolder: {options.InputFolder}");
                    Console.WriteLine($"OutputFolder: {options.OutputFolder}");
                    options = opts;
                })
                .WithNotParsed(errors =>
                {
                    Console.WriteLine("Invalid arguments provided.");
                    foreach (var error in errors)
                    {
                        Console.WriteLine(error.ToString());
                    }
                    System.Environment.Exit(1);
                });

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("Cancellation requested...");
                cts.Cancel();
                e.Cancel = true; // Prevent the process from terminating immediately
            };

            try
            {
                await ConvertOtlpTraceToTempoTrace(options, cts.Token);

                Console.WriteLine("Done.");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Operation was canceled.");
            }
        }

        private static async Task ConvertOtlpTraceToTempoTrace(CmdOptions options, CancellationToken cancel)
        {
            var parser = new OtlpTraceParser();
            var otlpTraceFiles = Directory.EnumerateFiles(options.InputFolder, "*.json", SearchOption.TopDirectoryOnly);
            foreach (var otlpTraceFile in otlpTraceFiles)
            {

                var parsedTempTraces = parser.TempoTraceFromOtlpJsonFile(otlpTraceFile);
                Console.WriteLine($"parsed {parsedTempTraces.Count} traces from otlp trace file {otlpTraceFile}...");
                if (!Directory.Exists(options.OutputFolder))
                {
                    Directory.CreateDirectory(options.OutputFolder);
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
                    var tempTraceFile = Path.Combine(options.OutputFolder, sanitizedFileName);
                    await File.WriteAllTextAsync(tempTraceFile, System.Text.Json.JsonSerializer.Serialize(root, parser.Options));
                }
            }
        }
    }
}
