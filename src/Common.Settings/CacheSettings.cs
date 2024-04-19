// -----------------------------------------------------------------------
// <copyright file="CacheSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Settings;

using System;

public class CacheSettings
{
    public BlobStorageSettings? BlobCache { get; set; }
    public FileCacheSettings? FileCache { get; set; }
    public MemoryCacheSettings? MemoryCache { get; set; }

    /// <summary>
    /// Gets or sets updated item still triggers cache invalidation within TTL
    /// </summary>
    public TimeSpan TimeToLive { get; set; } = TimeSpan.FromDays(15);
}