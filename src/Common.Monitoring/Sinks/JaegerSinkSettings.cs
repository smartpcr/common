// -----------------------------------------------------------------------
// <copyright file="JaegerSinkSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Sinks;

using System.ComponentModel.DataAnnotations;
using OpenTelemetry.Exporter;

public class JaegerSinkSettings
{
    public const string SettingName = $"{nameof(MonitorSettings)}:Sinks:Jaeger";

    [Required]
    public string Host { get; set; } = "localhost";
    [Required]
    public int Port { get; set; } = 16686;

    public void Configure(JaegerExporterOptions options)
    {
        options.AgentHost = Host;
        options.AgentPort = Port;
    }
}