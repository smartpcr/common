// -----------------------------------------------------------------------
// <copyright file="CacheExtensions.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache;

using Microsoft.Extensions.Caching.Distributed;

public static class CacheExtensions
{
    public static DistributedCacheEntryOptions PatchOptions(this DistributedCacheEntryOptions entry, DistributedCacheEntryOptions? options)
    {
        if (options != null)
        {
            entry.AbsoluteExpiration = options.AbsoluteExpiration ?? entry.AbsoluteExpiration;
            entry.AbsoluteExpirationRelativeToNow =
                options.AbsoluteExpirationRelativeToNow ?? entry.AbsoluteExpirationRelativeToNow;
            entry.SlidingExpiration = options.SlidingExpiration ?? entry.SlidingExpiration;
        }

        return entry;
    }
}