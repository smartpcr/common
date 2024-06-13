// -----------------------------------------------------------------------
// <copyright file="MetricSinkTypes.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Sinks;

using System;

[Flags]
public enum MetricSinkTypes
{
    None = 0,
    Console = 1 << 1,
    OTLP = 1 << 2,
    Prometheus = 1 << 3,
    ApplicationInsights = 1 << 4,
    Geneva = 1 << 5,
    File = 1 << 6,
    Default = Console | OTLP
}