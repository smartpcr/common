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
using Settings;

public class SpillableMemoryCache : IDistributedCache
{
    private readonly string cacheFolder;
    private readonly CacheSettings cacheSettings;
    private readonly ILogger<SpillableMemoryCache> logger;
    private readonly Tracer tracer;
    private readonly CacheMeter meter;
    private readonly MemoryCache memoryCache;

    public SpillableMemoryCache(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
    {
        logger = loggerFactory.CreateLogger<SpillableMemoryCache>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var metadata = configuration.GetConfiguredSettings<ApplicationMetadata>();
        var traceProvider = serviceProvider.GetRequiredService<TracerProvider>();
        tracer = traceProvider.GetTracer(metadata.ApplicationName + $".{nameof(SpillableMemoryCache)}", metadata.BuildVersion);
        meter = CacheMeter.Instance(metadata);

        cacheSettings = configuration.GetConfiguredSettings<CacheSettings>();
        cacheFolder = cacheSettings.FileCache!.CacheFolder;
        if (string.IsNullOrEmpty(cacheFolder) || !Directory.Exists(cacheFolder))
        {
            var binFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (binFolder == null)
            {
                throw new InvalidOperationException("invalid bin folder");
            }

            cacheFolder = Path.Combine(binFolder, cacheSettings.FileCache.CacheFolder);
            if (!Directory.Exists(cacheFolder))
            {
                logger.CreateCacheFolder(cacheFolder);
                Directory.CreateDirectory(cacheFolder);
            }
        }

        logger.SetCacheFolder(cacheFolder);

        var cacheOptions = new MemoryCacheOptions
        {
            CompactionPercentage = cacheSettings.MemoryCache!.CompactionPercentage,
            SizeLimit = cacheSettings.MemoryCache.SizeLimit,
            ExpirationScanFrequency = cacheSettings.TimeToLive
        };
        memoryCache = new MemoryCache(new OptionsWrapper<MemoryCacheOptions>(cacheOptions));
    }

    public byte[]? Get(string key)
    {
        using var _ = tracer.StartActiveSpan(nameof(Get));
        return GetAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = new CancellationToken())
    {
        using var _ = tracer.StartActiveSpan(nameof(GetAsync));
        logger.ReadCacheStart(key);
        var cacheDimensions = new[]
        {
            new KeyValuePair<string, object?>("name", nameof(SpillableMemoryCache)),
            new KeyValuePair<string, object?>("key", key)
        };

        try
        {
            if (memoryCache.TryGetValue(key, out var value) && value is byte[] cachedValue)
            {
                logger.ReadCacheStop(key);
                this.meter.IncrementCacheHits(cacheDimensions);
                return cachedValue;
            }

            this.meter.IncrementCacheMisses(cacheDimensions);

            var cacheFile = Path.Combine(cacheFolder, key);
            if (File.Exists(cacheFile))
            {
                if (File.GetCreationTimeUtc(cacheFile).Add(cacheSettings.TimeToLive) < DateTimeOffset.UtcNow)
                {
                    logger.ReadCacheFromFileStart(key);
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
                .SetSlidingExpiration(cacheSettings.TimeToLive);
            memoryCache.Set(key, fileContent, entryOptions);

            return fileContent;
        }
        catch (Exception ex)
        {
            logger.ReadCacheError(key, ex.Message);
            return null;
        }
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        using var _ = tracer.StartActiveSpan(nameof(Set));
        SetAsync(key, value, options).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task SetAsync(
        string key,
        byte[] value,
        DistributedCacheEntryOptions options,
        CancellationToken token = new CancellationToken())
    {
        using var _ = tracer.StartActiveSpan(nameof(SetAsync));
        logger.WriteCacheStart(key, value.Length);

        try
        {
            var size = (int)Math.Ceiling((double)value.Length / 1_000_000); // MB
            var entryOptions = new MemoryCacheEntryOptions()
                .SetSize(size)
                .SetSlidingExpiration(cacheSettings.TimeToLive);
            memoryCache.Set(key, value, entryOptions);
            var cacheFile = Path.Combine(cacheFolder, key);
            if (File.Exists(cacheFile))
            {
                File.Delete(cacheFile);
            }

            await File.WriteAllBytesAsync(cacheFile, value, token);
            logger.WriteCacheStop(key, value.Length);
        }
        catch (Exception ex)
        {
            logger.WriteCacheError(key, value.Length, ex.Message);
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
        using var _ = tracer.StartActiveSpan(nameof(Remove));
        RemoveAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public Task RemoveAsync(string key, CancellationToken token = new CancellationToken())
    {
        using var _ = tracer.StartActiveSpan(nameof(RemoveAsync));
        logger.RemoveCacheStart(key);
        try
        {
            memoryCache.Remove(key);
            var cacheFile = Path.Combine(cacheFolder, key);
            if (File.Exists(cacheFile))
            {
                File.Delete(cacheFile);
            }
        }
        catch (Exception ex)
        {
            logger.RemoveCacheError(key, ex.Message);
        }

        return Task.CompletedTask;
    }
}