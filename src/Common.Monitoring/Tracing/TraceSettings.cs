// -----------------------------------------------------------------------
// <copyright file="TraceSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Tracing;

using System.ComponentModel.DataAnnotations;
using Sinks;

public class TraceSettings
{
    [Required]
    public TraceSinkTypes SinkTypes { get; set; } = TraceSinkTypes.Default;
    public TraceSamplerTypes SamplerTypes { get; set; } = TraceSamplerTypes.AlwaysOn;
    public double SamplerRatio { get; set; } = 0.2;
    public bool IncludeAspNetCoreTrace { get; set; } = true;
}