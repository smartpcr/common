// -----------------------------------------------------------------------
// <copyright file="MetricsBuilder.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Metrics;

using System;
using System.Collections.Generic;
using Azure.Monitor.OpenTelemetry.Exporter;
using Config;
using Enrichment;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.R9.Extensions.HttpClient.Metering;
using Microsoft.R9.Extensions.Metering;
using Microsoft.R9.Extensions.Metering.Collectors;
using Microsoft.R9.Service.Middleware;
using OpenTelemetry.Exporter.Geneva;
using OpenTelemetry.Metrics;
using Sinks;

public static class MetricsBuilder
{
    public static IServiceCollection AddR9Metrics(this IServiceCollection services, IConfiguration configuration)
    {
        var metricsSettings = configuration.GetConfiguredSettings<MonitorSettings>().Metrics;
        Console.WriteLine($"Registering metrics with sink types {metricsSettings.SinkTypes}");

        services.AddEventCounterCollector(options =>
            {
                // https://learn.microsoft.com/en-us/dotnet/core/diagnostics/available-counters
                options.Counters.Add("System.Runtime", GetRuntimeCounters());
                Console.WriteLine("Event counter collector for System.Runtime enabled");
            })
            .AddHttpContextAccessor()
            .AddHttpMetering(builder =>
            {
                // incoming
                builder.AddMetricEnricher<MethodRequestMetricEnricher>();
                Console.WriteLine("Http metering enabled");
            });

        services.AddOpenTelemetry()
            .WithMetrics(builder =>
            {
                // registered IMeter and available to constructor injection
                builder//.AddMeter(metadata.ApplicationName) // open telemetry version
                    .AddMetering() // use r9 version for better injection, creation and include all sources, requires c# version 10 or later
                    .AddRuntimeInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation();
                Console.WriteLine("OpenTelemetry instrumentation enabled for Runtime, HttpClient and AspNetCore");

                if (metricsSettings.SinkTypes.HasFlag(MetricSinkTypes.Console))
                {
                    builder.AddConsoleExporter((_, readerOps) =>
                    {
                        readerOps.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 1000;
                        Console.WriteLine("Console metrics enabled");
                    });
                }

                if (metricsSettings.SinkTypes.HasFlag(MetricSinkTypes.OTLP))
                {
                    var oltpSinkSettings = configuration.GetConfiguredSettings<OtlpSinkSettings>(OtlpSinkSettings.SettingName);
                    builder.AddOtlpExporter(options =>
                    {
                        oltpSinkSettings.Configure(options);
                    });
                    Console.WriteLine("OTLP metrics enabled");
                }

                if (metricsSettings.SinkTypes.HasFlag(MetricSinkTypes.Prometheus))
                {
                    // Note: this uses prometheus-net.AspNetCore, which is not the official OpenTelemetry exporter
                    builder.AddPrometheusExporter();
                    Console.WriteLine("Prometheus metrics enabled");
                }

                if (metricsSettings.SinkTypes.HasFlag(MetricSinkTypes.Geneva))
                {
                    var genevaMetricSinkSettings = configuration.GetConfiguredSettings<GenevaMetricSinkSettings>(GenevaMetricSinkSettings.SettingName);
                    builder.AddGenevaMetricExporter(genevaMetricSinkSettings.Configure);
                    services.AddGenevaMetering(configuration.GetSection(GenevaMetricSinkSettings.GenevaMeterSettingName));
                    Console.WriteLine("Geneva metrics enabled");
                }

                if (metricsSettings.SinkTypes.HasFlag(MetricSinkTypes.ApplicationInsights))
                {
                    var appInsightsSinkSettings = configuration.GetConfiguredSettings<AppInsightsSinkSettings>(AppInsightsSinkSettings.SettingName);
                    builder.AddAzureMonitorMetricExporter(appInsightsSinkSettings.Configure);
                    Console.WriteLine("Application Insights metrics enabled");
                }

                if (metricsSettings.SinkTypes.HasFlag(MetricSinkTypes.File))
                {
                    var fileSink = configuration.GetConfiguredSettings<FileSinkSettings>(FileSinkSettings.MetricsSettingName);
                    builder.AddReader(new PeriodicExportingMetricReader(new MetricFileProcessor(fileSink)));
                    Console.WriteLine("File metrics enabled");
                }
            });

        return services;
    }

    public static void UseR9Metrics(this IApplicationBuilder app)
    {
        app.UseHttpMetering();
    }

    private static HashSet<string> GetRuntimeCounters()
    {
        return new HashSet<string>
        {
            "cpu-usage",
            "alloc-rate",
            "exception-count",
            "active-timer-count",
            "monitor-lock-contention-count",
            "threadpool-queue-length",
            "threadpool-thread-count",
            "working-set"
        };
    }
}