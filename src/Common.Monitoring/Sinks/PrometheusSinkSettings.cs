// -----------------------------------------------------------------------
// <copyright file="PrometheusSinkSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Sinks;

using System;
using OpenTelemetry.Exporter;

public class PrometheusSinkSettings
{
    public const string SettingName = $"{nameof(MonitorSettings)}:Sinks:Prometheus";

    public Uri Endpoint { get; set; } = new Uri("http://localhost:9090/");

    public void Configure(OtlpExporterOptions options)
    {
    }
}