// -----------------------------------------------------------------------
// <copyright file="KustoSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Settings;

using System;
using System.ComponentModel.DataAnnotations;

public class KustoSettings
{
    private Uri? clusterUrl;

    [Required]
    public string ClusterName { get; set; }
    public string RegionName { get; set; }
    [Required]
    public string DbName { get; set; }
    public string TableName { get; set; }
    public KustoAuthMode AuthMode { get; set; } = KustoAuthMode.Spn;
    public string KustoTableMapName { get; set; } = "JsonMap2";
    public AadSettings? Aad { get; set; }

    public Uri ClusterUrl
    {
        get =>
            clusterUrl ?? (string.IsNullOrEmpty(RegionName)
                ? new Uri($"https://{ClusterName}.kusto.windows.net")
                : new Uri($"https://{ClusterName}.{RegionName}.kusto.windows.net"));
        set => clusterUrl = value;
    }
}