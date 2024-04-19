// -----------------------------------------------------------------------
// <copyright file="CacheProvider.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Config;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.R9.Extensions.Metering;
using Newtonsoft.Json;
using OpenTelemetry.Trace;
using Settings;
using Storage.Blobs;
using DistributedCacheEntryOptions = Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions;

public class CacheProvider : ICacheProvider
{
    private readonly DistributedCacheEntryOptions cacheEntryOptions;
    private readonly ILogger<CacheProvider> logger;
    private readonly Tracer tracer;
    private readonly CacheMisses totalMisses;
    private readonly CacheExpires totalExpires;
    private readonly CacheErrors totalErrors;
    private readonly MultilayerCache multilayerCache;
    private readonly IBlobStorageClient? blobCacheClient;
    private readonly CacheSettings settings;
    private readonly JsonSerializer writeJsonSerializer;
    private readonly JsonSerializer readJsonSerializer;

    public CacheProvider(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
    {
        logger = loggerFactory.CreateLogger<CacheProvider>();
        writeJsonSerializer = new JsonSerializer
        {
            Formatting = Formatting.None,
            MaxDepth = 3,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            NullValueHandling = NullValueHandling.Ignore,
        };
        readJsonSerializer = new JsonSerializer
        {
            Formatting = Formatting.None,
            MaxDepth = 10,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            NullValueHandling = NullValueHandling.Ignore,
        };

        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        settings = configuration.GetConfiguredSettings<CacheSettings>();
        if (settings.MemoryCache == null)
        {
            throw new InvalidOperationException("Memory cache is not configured");
        }

        var metadata = configuration.GetConfiguredSettings<ApplicationMetadata>();
        var traceProvider = serviceProvider.GetRequiredService<TracerProvider>();
        tracer = traceProvider.GetTracer(metadata.ApplicationName + $".{nameof(CacheProvider)}", metadata.BuildVersion);
        IMeter meter = serviceProvider.GetRequiredService<IMeter>();
        totalMisses = CacheMeter.CreateCacheMisses(meter);
        totalExpires = CacheMeter.CreateCacheExpires(meter);
        totalErrors = CacheMeter.CreateCacheError(meter);

        cacheEntryOptions = new DistributedCacheEntryOptions { SlidingExpiration = settings.TimeToLive };

        multilayerCache = new MultilayerCache(new SpillableMemoryCache(serviceProvider, loggerFactory))
        {
            PopulateLayersOnGet = true
        };
        if (settings.BlobCache != null)
        {
            multilayerCache.AppendLayer(new BlobCache(serviceProvider, loggerFactory));
            blobCacheClient = new BlobStorageClient(
                serviceProvider,
                loggerFactory,
                new OptionsWrapper<BlobStorageSettings>(settings.BlobCache));
        }
    }

    public async Task<T> GetOrUpdateAsync<T>(
        string key,
        Func<Task<DateTimeOffset>> getLastModificationTime,
        Func<Task<T>> getItem,
        CancellationToken cancel) where T : class, new()
    {
        logger.ReadCacheStart(key);
        using var span = tracer.StartActiveSpan(nameof(GetOrUpdateAsync));
        var value = await multilayerCache.GetAsync(key, cancel);
        CachedItem<T>? cachedItem;

        async Task<T> RefreshItem()
        {
            var item = await getItem();
            cachedItem = new CachedItem<T>(item);
            string serializeObject = Serialize(cachedItem);
            value = Encoding.UTF8.GetBytes(serializeObject);
            logger.UpdateCacheStart(key);
            await multilayerCache.SetAsync(key, value, cacheEntryOptions, cancel);
            return item;
        }

        if (value == null)
        {
            totalMisses.Add(1, new CacheDimension(nameof(CacheProvider), key));
            return await RefreshItem();
        }

        try
        {
            var needRefresh = false;
            cachedItem = Deserialize<CachedItem<T>>(value);
            if (cachedItem == null)
            {
                needRefresh = true;
            }
            else
            {
                var lastUpdateTime = await getLastModificationTime();

                if ((lastUpdateTime != default && lastUpdateTime > cachedItem.CreatedOn) ||
                    cachedItem.CreatedOn.Add(settings.TimeToLive) < DateTimeOffset.UtcNow)
                {
                    totalExpires.Add(1, new CacheDimension(nameof(CacheProvider), key));
                    needRefresh = true;
                }
            }

            if (needRefresh)
            {
                var item = await RefreshItem();
                return item;
            }

            return cachedItem!.Value;
        }
        catch (Exception ex)
        {
            logger.CacheItemDeserializeError(typeof(T).Name, ex.Message);
            span.SetStatus(Status.Error);
            totalErrors.Add(1, new CacheDimension(nameof(CacheProvider), key));
            return await RefreshItem();
        }
    }

    public T GetOrUpdate<T>(string key, Func<DateTimeOffset> getLastModificationTime, Func<T> getItem, CancellationToken cancel = default) where T : class, new()
    {
        logger.ReadCacheStart(key);
        using var _ = tracer.StartActiveSpan(nameof(GetOrUpdate));
        var value = multilayerCache.Get(key);
        CachedItem<T>? cachedItem;

        T RefreshItem()
        {
            var item = getItem();
            cachedItem = new CachedItem<T>(item);
            string serializeObject = Serialize(cachedItem);
            value = Encoding.UTF8.GetBytes(serializeObject);
            multilayerCache.Set(key, value, cacheEntryOptions);
            return item;
        }

        if (value == null)
        {
            totalMisses.Add(1, new CacheDimension(nameof(CacheProvider), key));
            return RefreshItem();
        }

        cachedItem = Deserialize<CachedItem<T>>(value);
        var needRefresh = false;
        if (cachedItem == null)
        {
            needRefresh = true;
        }
        else
        {
            var lastUpdateTime = getLastModificationTime();
            if ((lastUpdateTime != default && lastUpdateTime > cachedItem.CreatedOn) ||
                cachedItem.CreatedOn.Add(settings.TimeToLive) < DateTimeOffset.UtcNow)
            {
                totalExpires.Add(1, new CacheDimension(nameof(CacheProvider), key));
                needRefresh = true;
            }
        }

        if (needRefresh)
        {
            return RefreshItem();
        }

        return cachedItem!.Value;
    }

    public bool TryGet<T>(string key, out T? item) where T : class, new()
    {
        logger.ReadCacheStart(key);
        using var _ = tracer.StartActiveSpan(nameof(TryGet));
        var value = multilayerCache.Get(key);
        if (value != null)
        {
            var cachedItem = Deserialize<CachedItem<T>>(value);
            if (cachedItem != null)
            {
                item = cachedItem.Value;
                return true;
            }
        }

        item = null;
        return false;
    }

    public async Task Set<T>(string key, T item, CancellationToken cancel) where T : class, new()
    {
        using var _ = tracer.StartActiveSpan(nameof(Set));
        var cachedItem = new CachedItem<T>(item);
        string serializeObject = Serialize(cachedItem);
        await multilayerCache.SetAsync(
            key,
            Encoding.UTF8.GetBytes(serializeObject),
            cacheEntryOptions,
            cancel);
    }

    public async Task ClearAsync(string key, CancellationToken cancel)
    {
        using var _ = tracer.StartActiveSpan(nameof(ClearAsync));
        await multilayerCache.RemoveAsync(key, cancel);
    }

    public async Task ClearAll(CancellationToken cancel)
    {
        using var _ = tracer.StartActiveSpan(nameof(ClearAll));
        if (blobCacheClient == null)
        {
            return;
        }

        var cachedItems = (await blobCacheClient.ListBlobNamesAsync(null, cancel)).ToList();
        var clearCacheTasks = new List<Task>();
        var totalToClear = cachedItems.Count;
        var totalCleared = 0;

        async Task ClearCacheItemTask(string key)
        {
            await ClearAsync(key, cancel);
            Interlocked.Increment(ref totalCleared);
            if (totalCleared % 100 == 0)
            {
                logger.ClearCache(totalCleared, totalToClear);
            }
        }

        foreach (var key in cachedItems)
        {
            clearCacheTasks.Add(ClearCacheItemTask(key));
        }

        await Task.WhenAll(clearCacheTasks.ToArray());
    }

    private string Serialize<T>(CachedItem<T>? cachedItem) where T : class, new()
    {
        var tempFile = Path.GetRandomFileName();
        if (File.Exists(tempFile))
        {
            File.Delete(tempFile);
        }

        using (var stream = File.OpenWrite(tempFile))
        {
            using var writer = new StreamWriter(stream);
            using var jsonWriter = new JsonTextWriter(writer);
            writeJsonSerializer.Serialize(jsonWriter, cachedItem);
            jsonWriter.Flush();
        }

        var serializedString = File.ReadAllText(tempFile);
        if (File.Exists(tempFile))
        {
            File.Delete(tempFile);
        }

        return serializedString;
    }

    private T? Deserialize<T>(byte[] data) where T : class, new()
    {
        var json = Encoding.UTF8.GetString(data);
        using var reader = new JsonTextReader(new StringReader(json));
        var instance = readJsonSerializer.Deserialize<T>(reader);
        return instance;
    }
}