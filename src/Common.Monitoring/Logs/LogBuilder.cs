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
using Microsoft.R9.Extensions.Logging;
using OpenTelemetry.Logs;
using Sinks;

public static class LogBuilder
{
    public static IServiceCollection AddR9Logging(this IServiceCollection services, IConfiguration configuration)
    {
        var monitorSettings = configuration.GetConfiguredSettings<MonitorSettings>();
        var logSettings = monitorSettings.Logs;
        Console.WriteLine($"registering logging with sink {logSettings.SinkTypes}");

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddOpenTelemetryLogging(options =>
            {
                options.IncludeScopes = true;
                options.UseFormattedMessage = true;
                options.IncludeStackTrace = true;
            }).AddOpenTelemetry(loggerOptions =>
            {
                if (logSettings.SinkTypes.HasFlag(LogSinkTypes.Console))
                {
                    loggerOptions.AddConsoleExporter();
                    Console.WriteLine("Console logging enabled");
                }

                if (logSettings.SinkTypes.HasFlag(LogSinkTypes.Geneva))
                {
                    var genevaLogSink = configuration.GetConfiguredSettings<GenevaLogSinkSettings>(GenevaLogSinkSettings.SettingName);
                    loggerOptions.AddGenevaLogExporter(genevaLogSink.Configure);
                    Console.WriteLine("Geneva logging enabled");
                }

                if (logSettings.SinkTypes.HasFlag(LogSinkTypes.ApplicationInsights))
                {
                    var appInsightsSinkSettings = configuration.GetConfiguredSettings<AppInsightsSinkSettings>(AppInsightsSinkSettings.SettingName);
                    loggerOptions.AddAzureMonitorLogExporter(appInsightsSinkSettings.Configure);
                    Console.WriteLine("Application Insights logging enabled");
                }

                if (logSettings.SinkTypes.HasFlag(LogSinkTypes.OTLP))
                {
                    var oltpSinkSettings = configuration.GetConfiguredSettings<OtlpSinkSettings>(OtlpSinkSettings.SettingName);
                    loggerOptions.AddOtlpExporter(options =>
                    {
                        oltpSinkSettings.Configure(options);
                    });
                    Console.WriteLine("OTLP logging enabled");
                }

                if (logSettings.SinkTypes.HasFlag(LogSinkTypes.File))
                {
                    var fileSink = configuration.GetConfiguredSettings<FileSinkSettings>(FileSinkSettings.LogSettingName);
                    loggerOptions.AddProcessor(new LogFileProcessor(fileSink));
                    Console.WriteLine("File logging enabled");
                }
            });
        });

        return services;
    }
}