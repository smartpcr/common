// -----------------------------------------------------------------------
// <copyright file="MultilayerCacheBuilder.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache;

using Microsoft.Extensions.DependencyInjection;

public static class MultilayerCacheBuilder
{
    public static void AddMultilayerCache(this IServiceCollection services)
    {
        services.AddSingleton<ICacheProvider, CacheProvider>();
    }
}