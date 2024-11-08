// -----------------------------------------------------------------------
// <copyright file="ICacheLayer.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Caching.Distributed;

    public interface ICacheLayer : IDistributedCache
    {
        CacheLayerType LayerType { get; }

        Task ClearAllAsync(CancellationToken cancel);
    }
}
