// -----------------------------------------------------------------------
// <copyright file="MemoryCacheWrapper.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Auth.Caching
{
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;

    public class MemoryCacheWrapper : IMemoryCache
    {
        private readonly MemoryCache cache;
        private readonly ILogger<IMemoryCache> logger;

        public MemoryCacheWrapper(MemoryCache cache, ILogger<IMemoryCache> logger)
        {
            this.cache = cache;
            this.logger = logger;
        }

        public ICacheEntry CreateEntry(object key)
        {
            this.logger.CreateEntryInMemory(key);

            return this.cache.CreateEntry(key);
        }

        public void Dispose()
        {
            this.cache.Dispose();
        }

        public void Remove(object key)
        {
            this.logger.RemoveEntryInMemory(key);

            this.cache.Remove(key);
        }

        public bool TryGetValue(object key, out object? value)
        {
            this.logger.GetEntryInMemory(key);

            return this.cache.TryGetValue(key, out value);
        }
    }
}