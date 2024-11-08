// -----------------------------------------------------------------------
// <copyright file="BlobCache.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Storage;
using Config;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using Storage.Blobs;

public class BlobCache : ICacheLayer
{
    private readonly IBlobStorageClient blobStorageClient;
    private readonly CacheSettings cacheSettings;
    private readonly ILogger<BlobCache> logger;
    private readonly Tracer tracer;
    private readonly CacheMeter meter;
    private readonly string tempFolder = Path.GetTempPath();

    public BlobCache(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, BlobStorageSettings blobStorageSettings)
    {
        this.logger = loggerFactory.CreateLogger<BlobCache>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var metadata = configuration.GetConfiguredSettings<ApplicationMetadata>();
        var traceProvider = serviceProvider.GetRequiredService<TracerProvider>();
        this.tracer = traceProvider.GetTracer(metadata.ApplicationName + $".{nameof(BlobCache)}", metadata.BuildVersion);
        this.meter = CacheMeter.Instance(metadata);
        this.cacheSettings = configuration.GetConfiguredSettings<CacheSettings>();
        this.blobStorageClient = new BlobStorageClient(serviceProvider,
            loggerFactory, new OptionsWrapper<BlobStorageSettings>(blobStorageSettings));
    }

    public CacheLayerType LayerType => CacheLayerType.Blob;

    public byte[]? Get(string key)
    {
        return this.GetAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = new())
    {
        using var _ = this.tracer.StartActiveSpan(nameof(this.GetAsync));
        this.logger.ReadCacheStart(key);
        var cacheDimensions = new[]
        {
            new KeyValuePair<string, object?>("name", nameof(BlobCache)),
            new KeyValuePair<string, object?>("key", key)
        };

        var tokenInfo = await this.blobStorageClient.GetBlobInfoAsync(key, token);
        if (tokenInfo == null)
        {
            this.logger.BlobCacheMiss(key);
            this.meter.IncrementCacheMisses(cacheDimensions);
            return null;
        }

        if (tokenInfo.CreatedOn.Add(this.cacheSettings.TimeToLive) < DateTimeOffset.UtcNow)
        {
            this.logger.BlobCacheExpired(key);
            this.meter.IncrementCacheExpires(cacheDimensions);
            return null;
        }

        await this.blobStorageClient.DownloadAsync(null, key, this.tempFolder, token);
        var downloadedBlogFile = Path.Combine(this.tempFolder, key);
        if (!File.Exists(downloadedBlogFile))
        {
            throw new InvalidOperationException($"blob download file not found: {downloadedBlogFile}");
        }

        var bytes = await File.ReadAllBytesAsync(downloadedBlogFile, token);
        File.Delete(downloadedBlogFile);
        this.logger.BlobCacheDownloaded(key);
        this.meter.IncrementCacheHits(cacheDimensions);
        return bytes;
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        using var _ = this.tracer.StartActiveSpan(nameof(this.Set));
        this.SetAsync(key, value, options).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = new())
    {
        using var _ = this.tracer.StartActiveSpan(nameof(this.SetAsync));
        var cacheDimensions = new[]
        {
            new KeyValuePair<string, object?>("name", nameof(BlobCache)),
            new KeyValuePair<string, object?>("key", key)
        };

        await blobStorageClient.UpsertAsync(key, value, null, token);
        this.meter.IncrementCacheWrites(cacheDimensions);
    }

    public void Refresh(string key)
    {
        throw new NotSupportedException();
    }

    public Task RefreshAsync(string key, CancellationToken token = new())
    {
        using var _ = this.tracer.StartActiveSpan(nameof(this.RefreshAsync));
        return Task.CompletedTask;
    }

    public void Remove(string key)
    {
        using var _ = this.tracer.StartActiveSpan(nameof(this.Remove));
        this.RemoveAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task RemoveAsync(string key, CancellationToken token = new())
    {
        using var _ = this.tracer.StartActiveSpan(nameof(this.RemoveAsync));
        try
        {
            await this.blobStorageClient.DeleteAsync(key, token);
        }
        catch (Exception ex)
        {
            this.logger.RemoveCacheError(key, ex.Message);
        }
    }

    public async Task ClearAllAsync(CancellationToken cancel)
    {
        using var _ = this.tracer.StartActiveSpan(nameof(this.ClearAllAsync));

        var cachedItems = (await this.blobStorageClient.ListBlobNamesAsync(null, cancel)).ToList();
        var clearCacheTasks = new List<Task>();
        var totalToClear = cachedItems.Count;
        var totalCleared = 0;

        async Task ClearCacheItemTask(string key)
        {
            await this.RemoveAsync(key, cancel);
            Interlocked.Increment(ref totalCleared);
            if (totalCleared % 100 == 0)
            {
                this.logger.ClearCache(totalCleared, totalToClear);
            }
        }

        foreach (var key in cachedItems)
        {
            clearCacheTasks.Add(ClearCacheItemTask(key));
        }

        await Task.WhenAll(clearCacheTasks.ToArray());
    }
}