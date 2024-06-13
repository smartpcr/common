// -----------------------------------------------------------------------
// <copyright file="ExtendedDistributedCacheWrapper.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Auth.Caching
{
    using System;
    using System.Buffers;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.Logging;
    using Microsoft.R9.Extensions.Caching;

    internal sealed class ExtendedDistributedCacheWrapper : IExtendedDistributedCache
    {
        private readonly FakeExtendedDistributedCache cache;
        private readonly ILogger<IExtendedDistributedCache> logger;

        public ExtendedDistributedCacheWrapper(
            FakeExtendedDistributedCache cache,
            ILogger<IExtendedDistributedCache> logger)
        {
            this.cache = cache;
            this.logger = logger;
        }

        public byte[]? Get(string key)
        {
            this.logger.GetTokenFromCache(key);

            return this.cache.Get(key);
        }

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            this.logger.GetTokenFromCache(key);

            return this.cache.GetAsync(key, token);
        }

        public void Refresh(string key)
        {
            this.cache.Refresh(key);
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            return this.cache.RefreshAsync(key, token);
        }

        public void Remove(string key)
        {
            this.logger.RemoveTokenFromCache(key);

            this.cache.Remove(key);
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            this.logger.RemoveTokenFromCache(key);

            return this.cache.RemoveAsync(key, token);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            this.logger.SetTokenInCache(key);

            this.cache.Set(key, value, options);
        }

        public Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token = default)
        {
            this.logger.SetTokenInCache(key);

            return this.cache.SetAsync(key, value, options, token);
        }

        public async Task SetMemoryAsync(
            string key,
            ReadOnlyMemory<byte> value,
            DistributedCacheEntryOptions options,
            CancellationToken cancellationToken)
        {
            this.logger.SetTokenInCache(key);

            await this.cache.SetAsync(key, value.ToArray(), options, cancellationToken);

            await this.cache.SetMemoryAsync(key, value, options, cancellationToken);
        }

        public Task<bool> TryGetAsync(string key, IBufferWriter<byte> destination, CancellationToken cancellationToken)
        {
            this.logger.GetTokenFromCache(key);

            return this.cache.TryGetAsync(key, destination, cancellationToken);
        }
    }
}