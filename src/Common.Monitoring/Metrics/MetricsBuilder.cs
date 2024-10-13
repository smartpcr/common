// -----------------------------------------------------------------------
// <copyright file="MetricsBuilder.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Metrics;

using System;
using Azure.Monitor.OpenTelemetry.Exporter;
using Config;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry;
using OpenTelemetry.Exporter.Geneva;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Sinks;

public static class MetricsBuilder
{
    public static IServiceCollection AddMetrics(this IServiceCollection services, IConfiguration configuration)
    {
        var metricsSettings = configuration.GetConfiguredSettings<MonitorSettings>().Metrics;
        var metadata = configuration.GetConfiguredSettings<ApplicationMetadata>();
        Console.WriteLine($"Registering metrics with sink types {metricsSettings.SinkTypes}");

        services.TryAdd(ServiceDescriptor.Singleton(typeof (Meter<>), typeof (Meter<>)));

        var builder = Sdk.CreateMeterProviderBuilder()
            .ConfigureResource(r => r.AddService(metadata.ApplicationName, serviceVersion: metadata.BuildVersion, serviceInstanceId: Environment.MachineName))
            .AddMeter(metadata.ApplicationName)
            .AddRuntimeInstrumentation()
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation();
        Console.WriteLine($"OpenTelemetry metrics enabled for Runtime, HttpClient, AspNetCore and {metadata.ApplicationName}");

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
            builder.AddReader(new PeriodicExportingMetricReader(
                new MetricFileExporter(fileSink),
                exportIntervalMilliseconds: metricsSettings.ExportIntervalMilliseconds));
            Console.WriteLine("File metrics enabled");
        }

        var meterProvider = builder.Build();
        services.AddSingleton(meterProvider);

        return services;
    }
}