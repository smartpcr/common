// -----------------------------------------------------------------------
// <copyright file="BlobStorageClient.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Storage.Blobs;

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Common.Config;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenTelemetry.Trace;

public class BlobStorageClient : IBlobStorageClient
{
    private readonly BlobContainerClient containerClient;
    private readonly BlobServiceClient blobService;
    private readonly ILogger<BlobStorageClient> logger;
    private readonly Tracer tracer;
    private readonly Func<string, BlobContainerClient> createClientFunc;

    public BlobStorageClient(
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory,
        IOptions<BlobStorageSettings>? blobStorageSettings = null)
    {
        logger = loggerFactory.CreateLogger<BlobStorageClient>();
        var config = serviceProvider.GetRequiredService<IConfiguration>();
        var metadata = config.GetConfiguredSettings<ApplicationMetadata>();
        var traceProvider = serviceProvider.GetRequiredService<TracerProvider>();
        tracer = traceProvider.GetTracer(metadata.ApplicationName + $".{nameof(BlobStorageClient)}", metadata.BuildVersion);
        var settings = blobStorageSettings?.Value ?? config.GetConfiguredSettings<BlobStorageSettings>();
        logger.AccessingBlob(settings.Account, settings.Container, settings.AuthMode.ToString());

        var factory = new BlobClientAuthHelper(serviceProvider, loggerFactory, settings);
        containerClient = factory.ContainerClient;
        blobService = factory.BlobService;
        createClientFunc = factory.CreateContainerClient;
    }

    private BlobStorageClient(BlobContainerClient containerClient, ILogger<BlobStorageClient> logger)
    {
        this.containerClient = containerClient;
        this.logger = logger;
    }

    public IBlobStorageClient SwitchContainer(string containerName)
    {
        using var _ = tracer.StartActiveSpan(nameof(SwitchContainer));
        var newClient = createClientFunc(containerName);
        return new BlobStorageClient(newClient, logger);
    }

    public async Task<IEnumerable<string>> ListContainersAsync(string prefix, CancellationToken cancel)
    {
        using var _ = tracer.StartActiveSpan(nameof(ListContainersAsync));
        var containerNames = new List<string>();
        var containers = blobService.GetBlobContainersAsync(prefix: prefix, cancellationToken: cancel);
        await using var containerEnumerator = containers.GetAsyncEnumerator(cancel);
        while (await containerEnumerator.MoveNextAsync())
        {
            if (containerEnumerator.Current != null)
            {
                containerNames.Add(containerEnumerator.Current.Name);
            }
        }

        return containerNames;
    }

    public async Task<IEnumerable<string>> ListBlobNamesAsync(DateTime? timeFilter, CancellationToken cancel)
    {
        using var _ = tracer.StartActiveSpan(nameof(ListBlobNamesAsync));
        await using var blobs = containerClient.GetBlobsAsync(cancellationToken: cancel).GetAsyncEnumerator(cancel);
        var output = new List<string>();
        while (await blobs.MoveNextAsync())
        {
            if (timeFilter.HasValue && blobs.Current.Properties.LastModified.HasValue)
            {
                if (blobs.Current.Properties.LastModified.Value.Date > timeFilter.Value)
                {
                    output.Add(blobs.Current.Name);
                }
            }
            else
            {
                output.Add(blobs.Current.Name);
            }
        }

        return output;
    }

    public async Task<string> DownloadBlobAsync(string blobName, CancellationToken cancel)
    {
        logger.DownloadBlobStart(blobName);
        var watch = Stopwatch.StartNew();
        using var _ = tracer.StartActiveSpan(nameof(DownloadBlobAsync));
        var blobClient = containerClient.GetBlobClient(blobName);
        await using var memoryStream = new MemoryStream();
        await blobClient.DownloadToAsync(memoryStream, cancel);
        var blobContent = Encoding.UTF8.GetString(memoryStream.ToArray());
        logger.DownloadBlobStop(blobName, watch.ElapsedMilliseconds);
        return blobContent;
    }

    public async Task<bool> ExistsAsync(string blobName, CancellationToken cancel)
    {
        logger.BlobExistsStart(blobName);
        using var _ = tracer.StartActiveSpan(nameof(ExistsAsync));
        var watch = Stopwatch.StartNew();
        var blobClient = containerClient.GetBlobClient(blobName);
        var exists = await blobClient.ExistsAsync(cancel);
        logger.BlobExistsStop(blobName, exists, watch.ElapsedMilliseconds);
        return exists;
    }

    public async Task<T?> GetAsync<T>(string blobName, CancellationToken cancel)
    {
        logger.GetBlobStart(blobName);
        var watch = Stopwatch.StartNew();
        using var _ = tracer.StartActiveSpan(nameof(GetAsync));
        var blobClient = containerClient.GetBlobClient(blobName);
        await using var memoryStream = new MemoryStream();
        await blobClient.DownloadToAsync(memoryStream, cancel);
        var blobContent = Encoding.UTF8.GetString(memoryStream.ToArray());

        if (typeof(T) == typeof(string))
        {
            return (T)Convert.ChangeType(blobContent, typeof(T));
        }

        T? obj = JsonConvert.DeserializeObject<T>(blobContent);
        if (obj is IBlobModel blobModel)
        {
            var props = await blobClient.GetPropertiesAsync(null, cancel);
            blobModel.ETag = props.Value.ETag.ToString();
        }

        logger.GetBlobStop(blobName, watch.ElapsedMilliseconds);
        return obj;
    }

    public async Task<object?> GetAsync(Type modelType, string blobName, CancellationToken cancel)
    {
        logger.GetBlobStart(blobName);
        var watch = Stopwatch.StartNew();
        using var _ = tracer.StartActiveSpan(nameof(GetAsync));
        var blobClient = containerClient.GetBlobClient(blobName);
        var downloadInfo = await blobClient.DownloadAsync(cancel);
        var tempFilePath = Path.GetRandomFileName();
        if (File.Exists(tempFilePath))
        {
            File.Delete(tempFilePath);
        }

        await using var fs = File.OpenWrite(tempFilePath);
        await downloadInfo.Value.Content.CopyToAsync(fs, cancel);
        var json = Encoding.UTF8.GetString(await File.ReadAllBytesAsync(tempFilePath, cancel));
        if (File.Exists(tempFilePath))
        {
            File.Delete(tempFilePath);
        }

        var obj = JsonConvert.DeserializeObject(json, modelType);
        logger.GetBlobStop(blobName, watch.ElapsedMilliseconds);
        return obj;
    }

    public async Task<List<T>> GetAllAsync<T>(string prefix, Func<string, bool> filter, CancellationToken cancellationToken)
    {
        using var _ = tracer.StartActiveSpan(nameof(GetAllAsync));
        await using var blobs = containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken).GetAsyncEnumerator(cancellationToken);
        var output = new List<T>();
        while (await blobs.MoveNextAsync())
        {
            if (filter == null || filter(blobs.Current.Name))
            {
                var blobClient = containerClient.GetBlobClient(blobs.Current.Name);
                var blobContent = await blobClient.DownloadAsync(cancellationToken);
                using var reader = new StreamReader(blobContent.Value.Content);
                var json = await reader.ReadToEndAsync(cancellationToken);
                T? obj = JsonConvert.DeserializeObject<T>(json);
                if (obj != null)
                {
                    output.Add(obj);
                }
            }
        }

        return output;
    }

    public async Task GetAllAsync(
        Type modelType,
        string prefix,
        Func<string, bool> filter,
        Func<IList<object>, CancellationToken, Task> onBatchReceived,
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        using var _ = tracer.StartActiveSpan(nameof(GetAllAsync));
        await using var blobs = containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken).GetAsyncEnumerator(cancellationToken);
        var batch = new List<object>();
        while (await blobs.MoveNextAsync())
        {
            if (filter == null || filter(blobs.Current.Name))
            {
                var blobClient = containerClient.GetBlobClient(blobs.Current.Name);
                var blobContent = await blobClient.DownloadAsync(cancellationToken);
                using var reader = new StreamReader(blobContent.Value.Content);
                var json = await reader.ReadToEndAsync(cancellationToken);
                var obj = JsonConvert.DeserializeObject(json, modelType);
                if (obj != null)
                {
                    batch.Add(obj);
                }

                if (batch.Count >= batchSize)
                {
                    await onBatchReceived(batch, cancellationToken);
                    batch = new List<object>();
                }
            }
        }

        if (batch.Count > 0)
        {
            await onBatchReceived(batch, cancellationToken);
            batch.Clear();
        }
    }

    public async Task<string> UploadAsync(string blobFolder, string blobName, string blobContent, string? etag = null, CancellationToken cancellationToken = default)
    {
        using var _ = tracer.StartActiveSpan(nameof(UploadAsync));
        var blobPath = string.IsNullOrEmpty(blobFolder)
            ? blobName
            : $"{blobFolder}/{blobName}";
        logger.UploadBlobStart(blobFolder, blobName);
        var watch = Stopwatch.StartNew();
        var blobClient = containerClient.GetBlobClient(blobPath);
        var eTag = string.IsNullOrEmpty(etag)
            ? default
            : new ETag(etag);
        if (!string.IsNullOrEmpty(etag))
        {
            var props = await blobClient.GetPropertiesAsync(new BlobRequestConditions { IfMatch = eTag }, cancellationToken);
            if (props?.Value.ETag.Equals(eTag) != true)
            {
                throw new InvalidOperationException($"Conflict, etag doesn't match");
            }
        }

        var uploadResponse = await blobClient.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(blobContent)), cancellationToken);
        logger.UploadBlobStop(blobFolder, blobName, watch.ElapsedMilliseconds);
        return uploadResponse.Value.ETag.ToString();
    }

    public async Task UploadBatchAsync<T>(string blobFolder, Func<T, string> getName, IList<T> list, CancellationToken cancellationToken)
    {
        logger.BulkUploadStart(list.Count, blobFolder);
        var watch = Stopwatch.StartNew();
        using var _ = tracer.StartActiveSpan(nameof(UploadBatchAsync));
        foreach (var item in list)
        {
            var blobName = getName(item);
            var blobPath = $"{blobFolder}/{blobName}";
            var blobClient = containerClient.GetBlobClient(blobPath);
            var blobContent = JsonConvert.SerializeObject(item);
            await blobClient.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(blobContent)), cancellationToken);
        }

        logger.BulkUploadStop(list.Count, blobFolder, watch.ElapsedMilliseconds);
    }

    public async Task<string> UpsertAsync(string blobName, byte[] content, string? etag = null, CancellationToken cancellationToken = default)
    {
        logger.UpsertStart(blobName, content.Length);
        using var _ = tracer.StartActiveSpan(nameof(UpsertAsync));
        var stopwatch = Stopwatch.StartNew();
        var blobClient = containerClient.GetBlobClient(blobName);
        var eTag = string.IsNullOrEmpty(etag)
            ? default
            : new ETag(etag);
        if (!string.IsNullOrEmpty(etag))
        {
            var props = await blobClient.GetPropertiesAsync(new BlobRequestConditions { IfMatch = eTag },
                cancellationToken); // this would throw exception with 412 status code
            if (props?.Value.ETag.Equals(eTag) != true)
            {
                throw new InvalidOperationException($"Conflict, etag doesn't match");
            }
        }

        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        var blobInfo = await blobClient.UploadAsync(new MemoryStream(content), cancellationToken);
        stopwatch.Stop();
        logger.LogDebug($"it took {stopwatch.Elapsed} to upload {blobName} ({content.Length / 1000}KB) to blob storage");
        return blobInfo.Value.ETag.ToString();
    }

    public async Task<string> DownloadAsync(string? blobFolder, string blobName, string localFolder, CancellationToken cancellationToken)
    {
        var blobPath = string.IsNullOrEmpty(blobFolder)
            ? blobName
            : $"{blobFolder}/{blobName}";
        logger.DownloadStart(blobPath, localFolder);
        using var _ = tracer.StartActiveSpan(nameof(DownloadAsync));
        var watch = Stopwatch.StartNew();
        var blobClient = containerClient.GetBlobClient(blobPath);
        var downloadInfo = await blobClient.DownloadAsync(cancellationToken);
        var filePath = Path.Combine(localFolder, blobName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        await using var fs = File.OpenWrite(filePath);
        await downloadInfo.Value.Content.CopyToAsync(fs, cancellationToken);
        logger.DownloadStop(blobPath, filePath, watch.ElapsedMilliseconds);
        return filePath;
    }

    public async Task<long> CountAsync<T>(string prefix, Func<T, bool> filter, CancellationToken cancellationToken)
    {
        using var _ = tracer.StartActiveSpan(nameof(CountAsync));
        await using var blobs = containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken).GetAsyncEnumerator(cancellationToken);
        var count = 0;
        while (await blobs.MoveNextAsync())
        {
            if (filter == null)
            {
                count++;
            }
            else
            {
                var blobClient = containerClient.GetBlobClient(blobs.Current.Name);
                var blobContent = await blobClient.DownloadAsync(cancellationToken);
                using var reader = new StreamReader(blobContent.Value.Content);
                var json = await reader.ReadToEndAsync(cancellationToken);
                var item = JsonConvert.DeserializeObject<T>(json);
                if (item != null)
                {
                    if (filter(item))
                    {
                        count++;
                    }
                }
            }
        }

        return count;
    }

    public async Task<long> CountAsync(string containerFilter, string blobFilter, CancellationToken cancel)
    {
        using var _ = tracer.StartActiveSpan(nameof(CountAsync));
        await using var blobs = containerClient.GetBlobsAsync(cancellationToken: cancel).GetAsyncEnumerator(cancel);
        var count = 0;
        var blobNameRegex = string.IsNullOrEmpty(blobFilter)
            ? new Regex(@"\w+")
            : new Regex(blobFilter, RegexOptions.IgnoreCase);
        while (await blobs.MoveNextAsync())
        {
            var blobName = blobs.Current.Name;
            if (blobNameRegex.IsMatch(blobName))
            {
                count++;
            }
        }

        return count;
    }

    public async Task<long> CountAsync(string prefix, CancellationToken cancel)
    {
        using var _ = tracer.StartActiveSpan(nameof(CountAsync));
        await using var blobs = containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancel).GetAsyncEnumerator(cancel);
        var count = 0;
        while (await blobs.MoveNextAsync())
        {
            count++;
        }

        return count;
    }

    public async Task<IList<T>> TryAcquireLeaseAsync<T>(string blobFolder, int take, Action<T> update, TimeSpan timeout)
    {
        logger.AcquireLeaseStart(blobFolder);
        using var _ = tracer.StartActiveSpan(nameof(TryAcquireLeaseAsync));
        await using var blobs = containerClient.GetBlobsAsync(prefix: blobFolder).GetAsyncEnumerator();

        timeout = timeout == default
            ? TimeSpan.FromMinutes(5)
            : timeout;
        var output = new Dictionary<string, T>();
        try
        {
            while (await blobs.MoveNextAsync())
            {
                var blobClient = containerClient.GetBlobClient(blobs.Current.Name);
                var leaseClient = blobClient.GetBlobLeaseClient();
                await leaseClient.AcquireAsync(timeout);
                var blobContent = await blobClient.DownloadAsync();
                using var reader = new StreamReader(blobContent.Value.Content);
                var json = await reader.ReadToEndAsync();
                var item = JsonConvert.DeserializeObject<T>(json);
                if (update != null && item != null)
                {
                    update(item);
                    await blobClient.UploadAsync(
                        new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item))),
                        CancellationToken.None);
                    output.Add(blobs.Current.Name, item);
                }

                if (take == 0 || output.Count >= take)
                {
                    break;
                }
            }

            return output.Values.ToList();
        }
        catch
        {
            logger.LeaseRejectedOnFolder(blobFolder);
            foreach (var blobName in output.Keys)
            {
                await ReleaseLeaseAsync(blobName);
            }

            return new List<T>();
        }
    }

    public async Task ReleaseLeaseAsync(string blobName)
    {
        logger.LeaseRejected(blobName);
        using var _ = tracer.StartActiveSpan(nameof(ReleaseLeaseAsync));
        var blobClient = containerClient.GetBlobClient(blobName);
        var leaseClient = blobClient.GetBlobLeaseClient();
        await leaseClient.ReleaseAsync();
    }

    public async Task DeleteBlobsAsync(string blobFolder, CancellationToken cancellationToken)
    {
        using var _ = tracer.StartActiveSpan(nameof(DeleteBlobsAsync));
        await using var blobs = containerClient.GetBlobsAsync(prefix: blobFolder, cancellationToken: cancellationToken).GetAsyncEnumerator(cancellationToken);
        var blobNames = new List<string>();
        while (await blobs.MoveNextAsync())
        {
            blobNames.Add(blobs.Current.Name);
        }

        foreach (var blobName in blobNames)
        {
            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots,
                cancellationToken: cancellationToken);
        }
    }

    public async Task DeleteAsync(string blobName, CancellationToken token)
    {
        using var _ = tracer.StartActiveSpan(nameof(DeleteAsync));
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: token);
    }

    public async Task<BlobInfo?> GetBlobInfoAsync(string blobName, CancellationToken cancel)
    {
        logger.LogDebug($"trying to get blob: {blobName}...");
        using var _ = tracer.StartActiveSpan(nameof(GetBlobInfoAsync));
        var blobClient = containerClient.GetBlobClient(blobName);
        if (!await blobClient.ExistsAsync(cancel))
        {
            return null;
        }

        var props = await blobClient.GetPropertiesAsync(null, cancel);
        var blobInfo = new BlobInfo
        {
            Name = blobName,
            Size = props.Value.ContentLength,
            CreatedOn = props.Value.CreatedOn,
            IsLeased = props.Value.LeaseState != LeaseState.Available &&
                       props.Value.LeaseState != LeaseState.Expired,
            TimeToLive = default // not supported
        };

        return blobInfo;
    }

    public async Task<DateTime> GetLastModificationTimeAsync(CancellationToken cancel)
    {
        using var _ = tracer.StartActiveSpan(nameof(GetLastModificationTimeAsync));
        await using var blobs = containerClient.GetBlobsAsync(cancellationToken: cancel).GetAsyncEnumerator(cancel);
        var lastModificationTime = DateTimeOffset.MinValue;
        while (await blobs.MoveNextAsync())
        {
            if (blobs.Current.Properties.LastModified.HasValue && blobs.Current.Properties.LastModified.Value > lastModificationTime)
            {
                lastModificationTime = blobs.Current.Properties.LastModified.Value;
            }
        }

        return lastModificationTime.DateTime;
    }
}