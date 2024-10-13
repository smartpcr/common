// -----------------------------------------------------------------------
// <copyright file="LogBuilder.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Logs;

using System;
using Azure.Monitor.OpenTelemetry.Exporter;
using Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using Sinks;

public static class LogBuilder
{
    public static IServiceCollection AddLogging(this IServiceCollection services, IConfiguration configuration)
    {
        var monitorSettings = configuration.GetConfiguredSettings<MonitorSettings>();
        var logSettings = monitorSettings.Logs;
        Console.WriteLine($"registering logging with sink {logSettings.SinkTypes}");

        var loggerFactory = CreateLoggerFactory(configuration);
        services.AddSingleton(loggerFactory);

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddOpenTelemetry(loggingOptions =>
            {
                if (logSettings.SinkTypes.HasFlag(LogSinkTypes.Console))
                {
                    loggingOptions.AddConsoleExporter();
                    Console.WriteLine("Console logging enabled");
                }

                if (logSettings.SinkTypes.HasFlag(LogSinkTypes.Geneva))
                {
                    var genevaLogSink = configuration.GetConfiguredSettings<GenevaLogSinkSettings>(GenevaLogSinkSettings.SettingName);
                    loggingOptions.AddGenevaLogExporter(genevaLogSink.Configure);
                    Console.WriteLine("Geneva logging enabled");
                }

                if (logSettings.SinkTypes.HasFlag(LogSinkTypes.OTLP))
                {
                    var oltpSinkSettings = configuration.GetConfiguredSettings<OtlpSinkSettings>(OtlpSinkSettings.SettingName);
                    loggingOptions.AddOtlpExporter(options =>
                    {
                        oltpSinkSettings.Configure(options);
                    });
                    Console.WriteLine("OTLP logging enabled");
                }

                if (logSettings.SinkTypes.HasFlag(LogSinkTypes.ApplicationInsights))
                {
                    var appInsightsSinkSettings = configuration.GetConfiguredSettings<AppInsightsSinkSettings>(AppInsightsSinkSettings.SettingName);
                    loggingOptions.AddAzureMonitorLogExporter(appInsightsSinkSettings.Configure);
                    Console.WriteLine("Application Insights logging enabled");
                }

                if (logSettings.SinkTypes.HasFlag(LogSinkTypes.File))
                {
                    var fileSink = configuration.GetConfiguredSettings<FileSinkSettings>(FileSinkSettings.LogSettingName);
                    var fileExporter = new LogFileExporter(fileSink, logSettings.LogLevel);
                    if (logSettings.UseBatch)
                    {
                        loggingOptions.AddProcessor(new BatchLogRecordExportProcessor(
                            fileExporter,
                            exporterTimeoutMilliseconds: logSettings.ExporterTimeoutMilliseconds));
                    }
                    else
                    {
                        loggingOptions.AddProcessor(new SimpleLogRecordExportProcessor(fileExporter));
                    }

                    Console.WriteLine("File logging enabled");
                }
            });
        });

        return services;
    }

    private static ILoggerFactory CreateLoggerFactory(IConfiguration configuration)
    {
        var monitorSettings = configuration.GetConfiguredSettings<MonitorSettings>();
        var logSettings = monitorSettings.Logs;

        return LoggerFactory.Create(builder =>
        {
            builder.AddOpenTelemetry(options =>
            {
                options.IncludeScopes = true;
                options.IncludeFormattedMessage = true;

                var fileSink = configuration.GetConfiguredSettings<FileSinkSettings>(FileSinkSettings.LogSettingName);
                var fileExporter = new LogFileExporter(fileSink, logSettings.LogLevel);
                if (logSettings.UseBatch)
                {
                    options.AddProcessor(new BatchLogRecordExportProcessor(
                        fileExporter,
                        exporterTimeoutMilliseconds: logSettings.ExporterTimeoutMilliseconds));
                }
                else
                {
                    options.AddProcessor(new SimpleLogRecordExportProcessor(fileExporter));
                }
                Console.WriteLine("File logging enabled");
            });
        });
    }
}