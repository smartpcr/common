// -----------------------------------------------------------------------
// <copyright file="TraceSinkTypes.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Sinks;

[System.Flags]
public enum TraceSinkTypes
{
    None = 0,
    OTLP = 1 << 1,
    Zipkin = 1 << 2,
    Jaeger = 1 << 3,
    Console = 1 << 4,
    Geneva = 1 << 5,
    ApplicationInsights = 1 << 6,
    File = 1 << 7,
    Default = OTLP | Console
}