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
}