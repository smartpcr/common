// -----------------------------------------------------------------------
// <copyright file="CacheProvider.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Config;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenTelemetry.Trace;
using DistributedCacheEntryOptions = Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions;

public class CacheProvider : ICacheProvider
{
    private readonly DistributedCacheEntryOptions cacheEntryOptions;
    private readonly ILogger<CacheProvider> logger;
    private readonly Tracer tracer;
    private readonly CacheMeter meter;
    private readonly MultilayerCache multilayerCache;
    private readonly CacheSettings settings;
    private readonly JsonSerializer writeJsonSerializer;
    private readonly JsonSerializer readJsonSerializer;

    public CacheProvider(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
    {
        this.logger = loggerFactory.CreateLogger<CacheProvider>();
        this.writeJsonSerializer = new JsonSerializer
        {
            Formatting = Formatting.None,
            MaxDepth = 3,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            NullValueHandling = NullValueHandling.Ignore,
        };
        this.readJsonSerializer = new JsonSerializer
        {
            Formatting = Formatting.None,
            MaxDepth = 10,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            NullValueHandling = NullValueHandling.Ignore,
        };

        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        this.settings = configuration.GetConfiguredSettings<CacheSettings>();
        if (this.settings.Local.MemoryCache == null)
        {
            throw new InvalidOperationException("Memory cache is not configured");
        }

        var metadata = configuration.GetConfiguredSettings<ApplicationMetadata>();
        var traceProvider = serviceProvider.GetRequiredService<TracerProvider>();
        this.tracer = traceProvider.GetTracer(metadata.ApplicationName + $".{nameof(CacheProvider)}", metadata.BuildVersion);
        this.meter = CacheMeter.Instance(metadata);

        this.cacheEntryOptions = new DistributedCacheEntryOptions { SlidingExpiration = this.settings.TimeToLive };

        this.multilayerCache = new MultilayerCache(new SpillableMemoryCache(serviceProvider, loggerFactory))
        {
            PopulateLayersOnGet = true
        };

        if (this.settings.Distributed is { CacheLayerType: CacheLayerType.Blob, Blob: not null })
        {
            this.multilayerCache.AppendLayer(new BlobCache(serviceProvider, loggerFactory, this.settings.Distributed.Blob));
        }
    }

    public async Task<T> GetOrUpdateAsync<T>(
        string key,
        Func<Task<DateTimeOffset>> getLastModificationTime,
        Func<Task<T>> getItem,
        CancellationToken cancel) where T : class, new()
    {
        this.logger.ReadCacheStart(key);
        using var span = this.tracer.StartActiveSpan(nameof(this.GetOrUpdateAsync));
        var value = await this.multilayerCache.GetAsync(key, cancel);
        CachedItem<T>? cachedItem;
        var cacheDimensions = new[]
        {
            new KeyValuePair<string, object?>("name", nameof(CacheProvider)),
            new KeyValuePair<string, object?>("key", key)
        };

        async Task<T> RefreshItem()
        {
            var item = await getItem();
            cachedItem = new CachedItem<T>(item);
            var serializeObject = this.Serialize(cachedItem);
            value = Encoding.UTF8.GetBytes(serializeObject);
            this.logger.UpdateCacheStart(key);
            await this.multilayerCache.SetAsync(key, value, this.cacheEntryOptions, cancel);
            return item;
        }

        if (value == null)
        {
            this.meter.IncrementCacheMisses(cacheDimensions);
            return await RefreshItem();
        }

        try
        {
            var needRefresh = false;
            cachedItem = this.Deserialize<CachedItem<T>>(value);
            if (cachedItem == null)
            {
                needRefresh = true;
            }
            else
            {
                var lastUpdateTime = await getLastModificationTime();

                if ((lastUpdateTime != default && lastUpdateTime > cachedItem.CreatedOn) ||
                    cachedItem.CreatedOn.Add(this.settings.TimeToLive) < DateTimeOffset.UtcNow)
                {
                    this.meter.IncrementCacheExpires(cacheDimensions);
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
            this.logger.CacheItemDeserializeError(typeof(T).Name, ex.Message);
            span.SetStatus(Status.Error);
            this.meter.IncrementCacheError(cacheDimensions);
            return await RefreshItem();
        }
    }

    public T GetOrUpdate<T>(string key, Func<DateTimeOffset> getLastModificationTime, Func<T> getItem, CancellationToken cancel = default) where T : class, new()
    {
        this.logger.ReadCacheStart(key);
        using var _ = this.tracer.StartActiveSpan(nameof(this.GetOrUpdate));
        var value = this.multilayerCache.Get(key);
        CachedItem<T>? cachedItem;
        var cacheDimensions = new[]
        {
            new KeyValuePair<string, object?>("name", nameof(CacheProvider)),
            new KeyValuePair<string, object?>("key", key)
        };

        T RefreshItem()
        {
            var item = getItem();
            cachedItem = new CachedItem<T>(item);
            var serializeObject = this.Serialize(cachedItem);
            value = Encoding.UTF8.GetBytes(serializeObject);
            this.multilayerCache.Set(key, value, this.cacheEntryOptions);
            return item;
        }

        if (value == null)
        {
            this.meter.IncrementCacheMisses(cacheDimensions);
            return RefreshItem();
        }

        cachedItem = this.Deserialize<CachedItem<T>>(value);
        var needRefresh = false;
        if (cachedItem == null)
        {
            needRefresh = true;
        }
        else
        {
            var lastUpdateTime = getLastModificationTime();
            if ((lastUpdateTime != default && lastUpdateTime > cachedItem.CreatedOn) ||
                cachedItem.CreatedOn.Add(this.settings.TimeToLive) < DateTimeOffset.UtcNow)
            {
                this.meter.IncrementCacheExpires(cacheDimensions);
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
        this.logger.ReadCacheStart(key);
        using var _ = this.tracer.StartActiveSpan(nameof(this.TryGet));
        var value = this.multilayerCache.Get(key);
        if (value != null)
        {
            var cachedItem = this.Deserialize<CachedItem<T>>(value);
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
        using var _ = this.tracer.StartActiveSpan(nameof(this.Set));
        var cachedItem = new CachedItem<T>(item);
        var serializeObject = this.Serialize(cachedItem);
        await this.multilayerCache.SetAsync(
            key,
            Encoding.UTF8.GetBytes(serializeObject), this.cacheEntryOptions,
            cancel);
    }

    public async Task ClearAsync(string key, CancellationToken cancel)
    {
        using var _ = this.tracer.StartActiveSpan(nameof(this.ClearAsync));
        await this.multilayerCache.RemoveAsync(key, cancel);
    }

    public async Task ClearAll(CancellationToken cancel)
    {
        using var _ = this.tracer.StartActiveSpan(nameof(this.ClearAll));
        await this.multilayerCache.ClearAll(cancel);
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
            this.writeJsonSerializer.Serialize(jsonWriter, cachedItem);
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
        var instance = this.readJsonSerializer.Deserialize<T>(reader);
        return instance;
    }
}