// -----------------------------------------------------------------------
// <copyright file="MetricsSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Metrics;

using Sinks;

public class MetricsSettings
{
    public MetricSinkTypes SinkTypes { get; set; } = MetricSinkTypes.Default;

    /// <summary>
    /// Gets or sets the interval in milliseconds at which the metrics are exported to the configured sinks.
    /// We override this value in test projects to make the tests run faster.
    /// </summary>
    public int ExportIntervalMilliseconds { get; set; } = 60000;

    public bool IncludeRuntimeMetrics { get; set; } = true;

    public bool IncludeAspNetCoreMetrics { get; set; } = true;

    public bool IncludeHttpMetrics { get; set; } = true;
}