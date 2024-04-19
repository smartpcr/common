// -----------------------------------------------------------------------
// <copyright file="DocDbSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.DocDb;

using System;
using System.ComponentModel.DataAnnotations;

public class DocDbSettings
{
    [Required]
    public string Account { get; set; }
    [Required]
    public string Db { get; set; }
    [Required]
    public string Collection { get; set; }
    public DocDbAuthMode AuthMode { get; set; }
    public string AuthKeySecret { get; set; }
    public bool CollectMetrics { get; set; }
    public Uri AccountUri => new Uri($"https://{Account}.documents.azure.com:443/");

    #region the following are only used when get key from msi
    public string SubscriptionId { get; set; }
    public string ResourceGroupName { get; set; }
    #endregion
}