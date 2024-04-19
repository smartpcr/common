// -----------------------------------------------------------------------
// <copyright file="ZipkinSinkSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Sinks;

using System;
using System.ComponentModel.DataAnnotations;
using OpenTelemetry.Exporter;

public class ZipkinSinkSettings
{
    public const string SettingName = $"{nameof(MonitorSettings)}:Sinks:Zipkin";

    /// <summary>
    /// Gets or sets zipkin endpoint, default port is 9411
    /// </summary>
    [Required]
    public Uri Endpoint { get; set; }

    public void Configure(ZipkinExporterOptions options)
    {
        options.Endpoint = Endpoint;
    }
}