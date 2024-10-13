// -----------------------------------------------------------------------
// <copyright file="LogSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Logs;

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using Sinks;

public class LogSettings
{
    [Required]
    public LogSinkTypes SinkTypes { get; set; }

    [Required]
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    public bool UseBatch { get; set; } = true;

    public int ExporterTimeoutMilliseconds { get; set; } = 30000;
}