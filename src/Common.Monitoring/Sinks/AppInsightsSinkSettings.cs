// -----------------------------------------------------------------------
// <copyright file="AppInsightsSinkSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Sinks;

using System.ComponentModel.DataAnnotations;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;

public class AppInsightsSinkSettings
{
    public const string SettingName = $"{nameof(MonitorSettings)}:Sinks:AppInsights";

    [Required]
    public string InstrumentationKey { get; set; }

    [Required]
    public string ConnectionString { get; set; }

    public bool RequireAzureCredential { get; set; }

    public void Configure(AzureMonitorExporterOptions options)
    {
        options.ConnectionString = $"InstrumentationKey={InstrumentationKey}";
        if (RequireAzureCredential)
        {
            options.Credential = new DefaultAzureCredential();
        }
    }
}