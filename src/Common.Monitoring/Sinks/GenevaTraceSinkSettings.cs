// -----------------------------------------------------------------------
// <copyright file="GenevaTraceSinkSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Sinks;

using System.ComponentModel.DataAnnotations;
using OpenTelemetry.Exporter.Geneva;

public class GenevaTraceSinkSettings
{
    public const string SettingName = $"{nameof(MonitorSettings)}:Sinks:Geneva:Trace";

    /// <summary>
    /// Gets or sets should match OneDSProvider name in agent configuration.
    /// i.e. "EtwSession=MyOpenTelemetryEtwSessionName"
    /// For linux, it should always be of the form "Endpoint=unix:unix_socket_path"
    /// i.e. "Endpoint=unix:/var/run/mdsd/default_fluent.socket"
    /// </summary>
    [Required]
    public string ConnectionString { get; set; }

    public void Configure(GenevaExporterOptions options)
    {
        options.ConnectionString = ConnectionString;
    }
}