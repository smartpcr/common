// -----------------------------------------------------------------------
// <copyright file="BlobCache.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Config;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.R9.Extensions.Metering;
using OpenTelemetry.Trace;
using Settings;
using Storage.Blobs;

public class BlobCache : IDistributedCache
{
    private readonly IBlobStorageClient blobStorageClient;
    private readonly CacheSettings cacheSettings;
    private readonly ILogger<BlobCache> logger;
    private readonly Tracer tracer;
    private readonly CacheMisses totalMisses;
    private readonly CacheHits totalHits;
    private readonly CacheWrites totalWrites;
    private readonly CacheExpires totalExpires;
    private readonly string tempFolder = Path.GetTempPath();

    public BlobCache(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
    {
        logger = loggerFactory.CreateLogger<BlobCache>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var metadata = configuration.GetConfiguredSettings<ApplicationMetadata>();
        var traceProvider = serviceProvider.GetRequiredService<TracerProvider>();
        tracer = traceProvider.GetTracer(metadata.ApplicationName + $".{nameof(BlobCache)}", metadata.BuildVersion);
        IMeter meter = serviceProvider.GetRequiredService<IMeter>();
        totalMisses = CacheMeter.CreateCacheMisses(meter);
        totalHits = CacheMeter.CreateCacheHits(meter);
        totalWrites = CacheMeter.CreateCacheWrites(meter);
        totalExpires = CacheMeter.CreateCacheExpires(meter);
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
        var tokenInfo = await blobStorageClient.GetBlobInfoAsync(key, token);
        if (tokenInfo == null)
        {
            logger.BlobCacheMiss(key);
            totalMisses.Add(1, new CacheDimension(nameof(BlobCache), key));
            return null;
        }

        if (tokenInfo.CreatedOn.Add(cacheSettings.TimeToLive) < DateTimeOffset.UtcNow)
        {
            logger.BlobCacheExpired(key);
            totalExpires.Add(1, new CacheDimension(nameof(BlobCache), key));
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
        totalHits.Add(1, new CacheDimension(nameof(BlobCache), key));
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
        await blobStorageClient.UpsertAsync(key, value, null, token);
        totalWrites.Add(1, new CacheDimension(nameof(BlobCache), key));
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