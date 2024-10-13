// -----------------------------------------------------------------------
// <copyright file="TraceBuilder.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Tracing;

using System;
using Azure.Monitor.OpenTelemetry.Exporter;
using Config;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter.Geneva;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sinks;

public static class TraceBuilder
{
    /// <summary>
    /// AddOpenTelemetry: This method initializes the OpenTelemetry SDK and registers the TracerProvider and
    /// other related services into the IServiceCollection.
    /// WithTracing: This method configures the TracerProvider with the specified settings and instrumentation.
    /// </summary>
    /// <param name="services">The service collection <see cref="IServiceCollection"/>.</param>
    /// <param name="configuration">The configuration, <see cref="IConfiguration"/></param>
    /// <returns><see cref="IServiceCollection"/></returns>
    public static IServiceCollection AddTracing(this IServiceCollection services, IConfiguration configuration)
    {
        var metadata = configuration.GetConfiguredSettings<ApplicationMetadata>();
        var traceSettings = configuration.GetConfiguredSettings<MonitorSettings>().Traces;
        Console.WriteLine($"registering tracing with sink types {traceSettings.SinkTypes}");

        services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource
                    .AddTelemetrySdk()
                    .AddService(
                        serviceName: metadata.ApplicationName,
                        serviceVersion: metadata.BuildVersion);
                Console.WriteLine($"OpenTelemetry resource configured for {metadata.ApplicationName} {metadata.BuildVersion}");
            })
            .WithTracing(builder =>
            {
                builder
                    .AddSource(metadata.ApplicationName)
                    .AddSource(metadata.ApplicationName + ".*") // make sure all traces starts with ApplicationName
                    .SetSampler(_ => CreateSampler(traceSettings))
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                    });
                Console.WriteLine($"Added tracing source {metadata.ApplicationName}.*");
                Console.WriteLine("Tracing instrumentation enabled for HttpClient and AspNetCore");

                if (traceSettings.SinkTypes.HasFlag(TraceSinkTypes.Console))
                {
                    builder.AddConsoleExporter();
                    Console.WriteLine("Console tracing enabled");
                }

                if (traceSettings.SinkTypes.HasFlag(TraceSinkTypes.Geneva))
                {
                    var genevaTraceSink = configuration.GetConfiguredSettings<GenevaTraceSinkSettings>(GenevaTraceSinkSettings.SettingName);
                    builder.AddGenevaTraceExporter(genevaTraceSink.Configure);
                    Console.WriteLine("Geneva tracing enabled");
                }

                if (traceSettings.SinkTypes.HasFlag(TraceSinkTypes.Zipkin))
                {
                    var zipSink = configuration.GetConfiguredSettings<ZipkinSinkSettings>(ZipkinSinkSettings.SettingName);
                    builder.AddZipkinExporter(zipSink.Configure);
                    Console.WriteLine("Zipkin tracing enabled");
                }

                if (traceSettings.SinkTypes.HasFlag(TraceSinkTypes.Jaeger))
                {
                    var jaegerSink = configuration.GetConfiguredSettings<JaegerSinkSettings>(JaegerSinkSettings.SettingName);
                    builder.AddJaegerExporter(jaegerSink.Configure);
                    Console.WriteLine("Jaeger tracing enabled");
                }

                if (traceSettings.SinkTypes.HasFlag(TraceSinkTypes.OTLP))
                {
                    var oltpSinkSettings = configuration.GetConfiguredSettings<OtlpSinkSettings>(OtlpSinkSettings.SettingName);
                    builder.AddOtlpExporter(options =>
                    {
                        oltpSinkSettings.Configure(options);
                    });
                    Console.WriteLine("OTLP tracing enabled");
                }

                if (traceSettings.SinkTypes.HasFlag(TraceSinkTypes.ApplicationInsights))
                {
                    var appInsightsSinkSettings = configuration.GetConfiguredSettings<AppInsightsSinkSettings>(AppInsightsSinkSettings.SettingName);
                    builder.AddAzureMonitorTraceExporter(appInsightsSinkSettings.Configure);
                    Console.WriteLine("Application Insights tracing enabled");
                }

                if (traceSettings.SinkTypes.HasFlag(TraceSinkTypes.File))
                {
                    var fileTraceSink = configuration.GetConfiguredSettings<FileSinkSettings>(FileSinkSettings.TraceSettingName);
                    builder.AddProcessor(new TraceFileProcessor(fileTraceSink));
                    Console.WriteLine("File tracing enabled");
                }
            });

        return services;
    }

    private static Sampler CreateSampler(TraceSettings traceSettings)
    {
        return traceSettings.SamplerTypes switch
        {
            TraceSamplerTypes.AlwaysOff => new AlwaysOffSampler(),
            TraceSamplerTypes.AlwaysOn => new AlwaysOnSampler(),
            TraceSamplerTypes.RatioBased => new TraceIdRatioBasedSampler(traceSettings.SamplerRatio),
            _ => throw new NotSupportedException($"Trace sampler type {traceSettings.SamplerTypes} is not supported."),
        };
    }
}