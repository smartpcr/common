// -----------------------------------------------------------------------
// <copyright file="MonitorSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring;

using System.ComponentModel.DataAnnotations;
using Logs;
using Metrics;
using Sinks;
using Tracing;

public class MonitorSettings
{
    [Required]
    public LogSettings Logs { get; set; }

    [Required]
    public TraceSettings Traces { get; set; }

    [Required]
    public MetricsSettings Metrics { get; set; }

    public bool UseOpenTelemetry()
    {
        return this.Logs.SinkTypes.HasFlag(LogSinkTypes.OTLP) || this.Traces.SinkTypes.HasFlag(TraceSinkTypes.OTLP) || this.Metrics.SinkTypes.HasFlag(MetricSinkTypes.OTLP);
    }
}