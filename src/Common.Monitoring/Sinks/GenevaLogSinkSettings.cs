// -----------------------------------------------------------------------
// <copyright file="GenevaLogSinkSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Sinks;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.R9.Extensions.HttpClient.Logging;
using OpenTelemetry.Exporter.Geneva;

/// <summary>
/// https://genevamondocs.azurewebsites.net/collect/instrument/OpenTelemetryDotNet/configurationoptions.html
/// </summary>
public class GenevaLogSinkSettings
{
    public const string SettingName = $"{nameof(MonitorSettings)}:Sinks:Geneva:Log";

    /// <summary>
    /// Gets or sets should match OneDSProvider name in agent configuration.
    /// i.e. "EtwSession=MyOpenTelemetryEtwSessionName"
    /// For linux, it should always be of the form "Endpoint=unix:unix_socket_path"
    /// i.e. "Endpoint=unix:/var/run/mdsd/default_fluent.socket"
    /// </summary>
    [Required]
    public string ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets used as tags for fast query
    /// </summary>
    public List<string> CustomFields { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets key-value pairs to be added to all telemetry
    /// </summary>
    public Dictionary<string, object> PrepopulatedFields { get; set; }

    public void Configure(GenevaExporterOptions options)
    {
        var customFiles = new List<string>
        {
            HttpClientLoggingDimensions.Host, // a separate column will be created in Geneva for each dimension provided here.
            HttpClientLoggingDimensions.Path,
            HttpClientLoggingDimensions.Duration,
            HttpClientLoggingDimensions.Method,
            HttpClientLoggingDimensions.StatusCode,
            HttpClientLoggingDimensions.ResponseBody,
            $"{HttpClientLoggingDimensions.RequestHeaderPrefix}Accept",
            $"{HttpClientLoggingDimensions.RequestHeaderPrefix}Authorization",
        };
        if (CustomFields.Any())
        {
            customFiles.AddRange(CustomFields);
        }

        options.ConnectionString = ConnectionString;
        options.CustomFields = customFiles;
        options.PrepopulatedFields = PrepopulatedFields;
    }
}