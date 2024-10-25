// -----------------------------------------------------------------------
// <copyright file="BlobCache.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache;

using System;
using System.Collections.Generic;
using System.IO;
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

public class BlobCache : IDistributedCache
{
    private readonly IBlobStorageClient blobStorageClient;
    private readonly CacheSettings cacheSettings;
    private readonly ILogger<BlobCache> logger;
    private readonly Tracer tracer;
    private readonly CacheMeter meter;
    private readonly string tempFolder = Path.GetTempPath();

    public BlobCache(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
    {
        logger = loggerFactory.CreateLogger<BlobCache>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var metadata = configuration.GetConfiguredSettings<ApplicationMetadata>();
        var traceProvider = serviceProvider.GetRequiredService<TracerProvider>();
        tracer = traceProvider.GetTracer(metadata.ApplicationName + $".{nameof(BlobCache)}", metadata.BuildVersion);
        meter = CacheMeter.Instance(metadata);
        cacheSettings = configuration.GetConfiguredSettings<CacheSettings>();
        blobStorageClient = new BlobStorageClient(serviceProvider,
            loggerFactory,
            new OptionsWrapper<BlobStorageSettings>(cacheSettings.BlobCache!));
    }

    public byte[]? Get(string key)
    {
        return GetAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = new CancellationToken())
    {
        using var _ = tracer.StartActiveSpan(nameof(GetAsync));
        logger.ReadCacheStart(key);
        var cacheDimensions = new[]
        {
            new KeyValuePair<string, object?>("name", nameof(BlobCache)),
            new KeyValuePair<string, object?>("key", key)
        };

        var tokenInfo = await blobStorageClient.GetBlobInfoAsync(key, token);
        if (tokenInfo == null)
        {
            logger.BlobCacheMiss(key);
            this.meter.IncrementCacheMisses(cacheDimensions);
            return null;
        }

        if (tokenInfo.CreatedOn.Add(cacheSettings.TimeToLive) < DateTimeOffset.UtcNow)
        {
            logger.BlobCacheExpired(key);
            this.meter.IncrementCacheExpires(cacheDimensions);
            return null;
        }

        await blobStorageClient.DownloadAsync(null, key, tempFolder, token);
        var downloadedBlogFile = Path.Combine(tempFolder, key);
        if (!File.Exists(downloadedBlogFile))
        {
            throw new InvalidOperationException($"blob download file not found: {downloadedBlogFile}");
        }

        var bytes = await File.ReadAllBytesAsync(downloadedBlogFile, token);
        File.Delete(downloadedBlogFile);
        logger.BlobCacheDownloaded(key);
        this.meter.IncrementCacheHits(cacheDimensions);
        return bytes;
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        using var _ = tracer.StartActiveSpan(nameof(Set));
        SetAsync(key, value, options).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = new CancellationToken())
    {
        using var _ = tracer.StartActiveSpan(nameof(SetAsync));
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

    public Task RefreshAsync(string key, CancellationToken token = new CancellationToken())
    {
        using var _ = tracer.StartActiveSpan(nameof(RefreshAsync));
        return Task.CompletedTask;
    }

    public void Remove(string key)
    {
        using var _ = tracer.StartActiveSpan(nameof(Remove));
        RemoveAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task RemoveAsync(string key, CancellationToken token = new CancellationToken())
    {
        using var _ = tracer.StartActiveSpan(nameof(RemoveAsync));
        try
        {
            await blobStorageClient.DeleteAsync(key, token);
        }
        catch (Exception ex)
        {
            logger.RemoveCacheError(key, ex.Message);
        }
    }
}