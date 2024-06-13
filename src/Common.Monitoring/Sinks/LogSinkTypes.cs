// -----------------------------------------------------------------------
// <copyright file="LogSinkTypes.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Sinks;

using System;

[Flags]
public enum LogSinkTypes
{
    None = 0,
    Console = 1 << 1,
    File = 1 << 2,
    Geneva = 1 << 3,
    ApplicationInsights = 1 << 4,
    OTLP = 1 << 5,
    OneDS = ApplicationInsights | Geneva,
    Default = OTLP | Console,
}