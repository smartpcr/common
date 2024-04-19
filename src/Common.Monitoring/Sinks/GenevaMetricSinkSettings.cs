// -----------------------------------------------------------------------
// <copyright file="GenevaMetricSinkSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Sinks;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.R9.Extensions.Metering;
using OpenTelemetry.Exporter.Geneva;

public class GenevaMetricSinkSettings
{
    public const string SettingName = $"{nameof(MonitorSettings)}:Sinks:Geneva:Metrics";
    public const string GenevaMeterSettingName = $"{nameof(MonitorSettings)}:Sinks:Geneva:Metrics:GenevaMetering";

    public string ChannelName { get; set; } = Environment.OSVersion.Platform == PlatformID.Win32NT
        ? "Microsoft.Geneva.OneDS"
        : "/var/run/mdsd/default_fluent.socket";

    public Dictionary<string, object> GlobalTags { get; set; }

    [Required]
    public GenevaMeteringOptions GenevaMetering { get; set; }

    public void Configure(GenevaMetricExporterOptions options)
    {
        options.ConnectionString = ChannelName;
        options.PrepopulatedMetricDimensions = GlobalTags;
    }
}