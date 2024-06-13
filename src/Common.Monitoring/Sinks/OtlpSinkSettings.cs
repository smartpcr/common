// -----------------------------------------------------------------------
// <copyright file="OtlpSinkSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Sinks;

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Logs;
using OpenTelemetry;
using OpenTelemetry.Exporter;

public class OtlpSinkSettings
{
    public const string SettingName = $"{nameof(MonitorSettings)}:Sinks:Otlp";

    [Required]
    public Uri Endpoint { get; set; }

    public OtlpExportProtocol Protocol { get; set; } = OtlpExportProtocol.HttpProtobuf;

    public ExportProcessorType ProcessorType { get; set; } = ExportProcessorType.Simple;

    public string LogEndpoint { get; set; } = "/v1/logs";

    /// <summary>
    /// Gets or sets timeout in milliseconds to push tracing // default is 10 seconds
    /// </summary>
    public int TimeoutMilliseconds { get; set; } = 10000;

    public int MaxQueueSize { get; set; } = 1000;

    public int DelayMilliseconds { get; set; } = 5000;

    public int BatchSize { get; set; } = 512;

    internal void Configure(
        OtlpExporterOptions options,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Console.WriteLine($"Configure OTLP exporter for {memberName} in {callerFile}:{lineNumber}");
        options.Endpoint = Endpoint;
        if (memberName == nameof(LogBuilder) && string.IsNullOrEmpty(LogEndpoint))
        {
            options.Endpoint = new Uri(Endpoint, LogEndpoint);
        }

        options.Protocol = Protocol;
        options.ExportProcessorType = ProcessorType;
        options.TimeoutMilliseconds = TimeoutMilliseconds; // default is 10 seconds
        options.BatchExportProcessorOptions = new BatchExportProcessorOptions<Activity>
        {
            MaxQueueSize = MaxQueueSize,
            ScheduledDelayMilliseconds = DelayMilliseconds,
            ExporterTimeoutMilliseconds = TimeoutMilliseconds,
            MaxExportBatchSize = BatchSize,
        };
    }
}