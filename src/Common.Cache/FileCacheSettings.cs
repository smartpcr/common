// -----------------------------------------------------------------------
// <copyright file="FileCacheSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache;

using System.ComponentModel.DataAnnotations;

public class FileCacheSettings
{
    [Required]
    public string CacheFolder { get; set; } = "cache";
}