// -----------------------------------------------------------------------
// <copyright file="LocalCacheSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache
{
    public class LocalCacheSettings
    {
        public MemoryCacheSettings MemoryCache { get; set; }
        public FileCacheSettings FileCache { get; set; }
    }
}
