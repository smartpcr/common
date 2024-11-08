// -----------------------------------------------------------------------
// <copyright file="SpillableMemoryCache.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Config;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;

public class SpillableMemoryCache : ICacheLayer
{
    private readonly string cacheFolder;
    private readonly CacheSettings cacheSettings;
    private readonly ILogger<SpillableMemoryCache> logger;
    private readonly Tracer tracer;
    private readonly CacheMeter meter;
    private readonly MemoryCache memoryCache;

    public SpillableMemoryCache(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
    {
        this.logger = loggerFactory.CreateLogger<SpillableMemoryCache>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var metadata = configuration.GetConfiguredSettings<ApplicationMetadata>();
        var traceProvider = serviceProvider.GetRequiredService<TracerProvider>();
        this.tracer = traceProvider.GetTracer(metadata.ApplicationName + $".{nameof(SpillableMemoryCache)}", metadata.BuildVersion);
        this.meter = CacheMeter.Instance(metadata);

        this.cacheSettings = configuration.GetConfiguredSettings<CacheSettings>();
        this.cacheFolder = this.cacheSettings.Local.FileCache.CacheFolder;
        if (string.IsNullOrEmpty(this.cacheFolder) || !Directory.Exists(this.cacheFolder))
        {
            var binFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (binFolder == null)
            {
                throw new InvalidOperationException("invalid bin folder");
            }

            this.cacheFolder = Path.Combine(binFolder, this.cacheSettings.Local.FileCache.CacheFolder);
            if (!Directory.Exists(this.cacheFolder))
            {
                this.logger.CreateCacheFolder(this.cacheFolder);
                Directory.CreateDirectory(this.cacheFolder);
            }
        }

        this.logger.SetCacheFolder(this.cacheFolder);

        var cacheOptions = new MemoryCacheOptions
        {
            CompactionPercentage = this.cacheSettings.Local.MemoryCache.CompactionPercentage,
            SizeLimit = this.cacheSettings.Local.MemoryCache.SizeLimit,
            ExpirationScanFrequency = this.cacheSettings.TimeToLive
        };
        this.memoryCache = new MemoryCache(new OptionsWrapper<MemoryCacheOptions>(cacheOptions));
    }

    public CacheLayerType LayerType => CacheLayerType.Local;

    public byte[]? Get(string key)
    {
        using var _ = this.tracer.StartActiveSpan(nameof(this.Get));
        return this.GetAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = new CancellationToken())
    {
        using var _ = this.tracer.StartActiveSpan(nameof(this.GetAsync));
        this.logger.ReadCacheStart(key);
        var cacheDimensions = new[]
        {
            new KeyValuePair<string, object?>("name", nameof(SpillableMemoryCache)),
            new KeyValuePair<string, object?>("key", key)
        };

        try
        {
            if (this.memoryCache.TryGetValue(key, out var value) && value is byte[] cachedValue)
            {
                this.logger.ReadCacheStop(key);
                this.meter.IncrementCacheHits(cacheDimensions);
                return cachedValue;
            }

            this.meter.IncrementCacheMisses(cacheDimensions);

            var cacheFile = Path.Combine(this.cacheFolder, key);
            if (File.Exists(cacheFile))
            {
                if (File.GetCreationTimeUtc(cacheFile).Add(this.cacheSettings.TimeToLive) < DateTimeOffset.UtcNow)
                {
                    this.logger.ReadCacheFromFileStart(key);
                    this.meter.IncrementCacheExpires(cacheDimensions);
                    return null;
                }
            }
            else
            {
                this.meter.IncrementCacheMisses(cacheDimensions);
                return null;
            }

            var fileContent = await File.ReadAllBytesAsync(cacheFile, token);
            this.meter.IncrementCacheHits(cacheDimensions);
            var size = (int)Math.Ceiling((double)fileContent.Length / 1_000_000); // MB
            var entryOptions = new MemoryCacheEntryOptions()
                .SetSize(size)
                .SetSlidingExpiration(this.cacheSettings.TimeToLive);
            this.memoryCache.Set(key, fileContent, entryOptions);

            return fileContent;
        }
        catch (Exception ex)
        {
            this.logger.ReadCacheError(key, ex.Message);
            return null;
        }
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        using var _ = this.tracer.StartActiveSpan(nameof(this.Set));
        this.SetAsync(key, value, options).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task SetAsync(
        string key,
        byte[] value,
        DistributedCacheEntryOptions options,
        CancellationToken token = new CancellationToken())
    {
        using var _ = this.tracer.StartActiveSpan(nameof(this.SetAsync));
        this.logger.WriteCacheStart(key, value.Length);

        try
        {
            var size = (int)Math.Ceiling((double)value.Length / 1_000_000); // MB
            var entryOptions = new MemoryCacheEntryOptions()
                .SetSize(size)
                .SetSlidingExpiration(this.cacheSettings.TimeToLive);
            this.memoryCache.Set(key, value, entryOptions);
            var cacheFile = Path.Combine(this.cacheFolder, key);
            if (File.Exists(cacheFile))
            {
                File.Delete(cacheFile);
            }

            await File.WriteAllBytesAsync(cacheFile, value, token);
            this.logger.WriteCacheStop(key, value.Length);
        }
        catch (Exception ex)
        {
            this.logger.WriteCacheError(key, value.Length, ex.Message);
        }
    }

    public void Refresh(string key)
    {
        throw new NotSupportedException();
    }

    public Task RefreshAsync(string key, CancellationToken token = new CancellationToken())
    {
        return Task.CompletedTask;
    }

    public void Remove(string key)
    {
        using var _ = this.tracer.StartActiveSpan(nameof(this.Remove));
        this.RemoveAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public Task RemoveAsync(string key, CancellationToken token = new CancellationToken())
    {
        using var _ = this.tracer.StartActiveSpan(nameof(this.RemoveAsync));
        this.logger.RemoveCacheStart(key);
        try
        {
            this.memoryCache.Remove(key);
            var cacheFile = Path.Combine(this.cacheFolder, key);
            if (File.Exists(cacheFile))
            {
                File.Delete(cacheFile);
            }
        }
        catch (Exception ex)
        {
            this.logger.RemoveCacheError(key, ex.Message);
        }

        return Task.CompletedTask;
    }

    public Task ClearAllAsync(CancellationToken cancel)
    {
        throw new NotImplementedException();
    }
}