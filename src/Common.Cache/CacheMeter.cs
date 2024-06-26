// -----------------------------------------------------------------------
// <copyright file="CacheMeter.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache;

using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.AmbientMetadata;

internal class CacheMeter
{
    private readonly Counter<long> _cacheHits;
    private readonly Counter<long> _cacheMisses;
    private readonly Counter<long> _cacheWrites;
    private readonly Counter<long> _cacheExpires;
    private readonly Counter<long> _cacheErrors;

    private CacheMeter(ApplicationMetadata metadata)
    {
        var meter = new Meter($"{metadata.ApplicationName}.{nameof(CacheMeter)}", metadata.BuildVersion);
        this._cacheHits = meter.CreateCounter<long>("cache_hits", "Number of cache hits");
        this._cacheMisses = meter.CreateCounter<long>("cache_misses", "Number of cache misses");
        this._cacheWrites = meter.CreateCounter<long>("cache_writes", "Number of cache writes");
        this._cacheExpires = meter.CreateCounter<long>("cache_expires", "Number of cache expires");
        this._cacheErrors = meter.CreateCounter<long>("cache_errors", "Number of cache errors");
    }

    public static CacheMeter Instance(ApplicationMetadata metadata)
    {
        return new CacheMeter(metadata);
    }

    public void IncrementCacheMisses(params KeyValuePair<string, object?>[] dimensions)
    {
        this._cacheMisses.Add(1, dimensions);
    }

    public void IncrementCacheHits(params KeyValuePair<string, object?>[] dimensions)
    {
        this._cacheHits.Add(1, dimensions);
    }

    public void IncrementCacheWrites(params KeyValuePair<string, object?>[] dimensions)
    {
        this._cacheWrites.Add(1, dimensions);
    }

    public void IncrementCacheExpires(params KeyValuePair<string, object?>[] dimensions)
    {
        this._cacheExpires.Add(1, dimensions);
    }

    public void IncrementCacheError(params KeyValuePair<string, object?>[] dimensions)
    {
        this._cacheErrors.Add(1, dimensions);
    }
}