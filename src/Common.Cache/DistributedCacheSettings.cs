// -----------------------------------------------------------------------
// <copyright file="DistributedCacheSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache
{
    using Common.Storage;

    public class DistributedCacheSettings
    {
        public CacheLayerType CacheLayerType { get; set; } = CacheLayerType.Redis;
        public RedisCacheSettings? Redis { get; set; }
        public BlobStorageSettings? Blob { get; set; }
    }
}
