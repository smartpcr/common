// -----------------------------------------------------------------------
// <copyright file="CacheMeter.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache;

using Microsoft.R9.Extensions.Metering;

internal static partial class CacheMeter
{
    [Counter(typeof(CacheDimension))]
    public static partial CacheMisses CreateCacheMisses(IMeter meter);

    [Counter(typeof(CacheDimension))]
    public static partial CacheHits CreateCacheHits(IMeter meter);

    [Counter(typeof(CacheDimension))]
    public static partial CacheWrites CreateCacheWrites(IMeter meter);

    [Counter(typeof(CacheDimension))]
    public static partial CacheExpires CreateCacheExpires(IMeter meter);

    [Counter(typeof(CacheDimension))]
    public static partial CacheErrors CreateCacheError(IMeter meter);
}