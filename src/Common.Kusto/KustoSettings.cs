// -----------------------------------------------------------------------
// <copyright file="KustoSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Kusto;

using System.ComponentModel.DataAnnotations;
using Common.Auth;

public class KustoSettings
{
    private string? clusterUrl;

    public string ClusterName { get; set; }

    public string RegionName { get; set; }

    [Required]
    public string DbName { get; set; }

    public string TableName { get; set; }

    [Required]
    public KustoAuthMode AuthMode { get; set; } = KustoAuthMode.Spn;

    public string KustoTableMapName { get; set; } = "JsonMap2";

    public AadSettings? Aad { get; set; }

    public string ClusterUrl
    {
        get =>
            clusterUrl ?? (string.IsNullOrEmpty(RegionName)
                ? $"https://{ClusterName}.kusto.windows.net"
                : $"https://{ClusterName}.{RegionName}.kusto.windows.net");
        set => clusterUrl = value;
    }
}