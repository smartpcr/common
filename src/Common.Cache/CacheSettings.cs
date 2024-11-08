// -----------------------------------------------------------------------
// <copyright file="CacheSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache;

using System;
using Common.Storage;

public class CacheSettings
{
    public LocalCacheSettings Local { get; set; }

    public DistributedCacheSettings Distributed { get; set; }

    /// <summary>
    /// Gets or sets updated item still triggers cache invalidation within TTL
    /// </summary>
    public TimeSpan TimeToLive { get; set; } = TimeSpan.FromDays(15);
}