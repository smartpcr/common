// -----------------------------------------------------------------------
// <copyright file="IBlobStorageClient.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Storage.Blobs;

public interface IBlobStorageClient
{
    /// <summary>
    /// create a new instance of client
    /// </summary>
    /// <param name="containerName">The container name</param>
    /// <returns><see cref="IBlobStorageClient"/>instance of blob client</returns>
    IBlobStorageClient SwitchContainer(string containerName);

    Task<IEnumerable<string>> ListContainersAsync(string prefix, CancellationToken cancel);

    Task<IEnumerable<string>> ListBlobNamesAsync(DateTime? timeFilter, CancellationToken cancel);

    Task<string> DownloadBlobAsync(string blobName, CancellationToken cancel);

    Task<bool> ExistsAsync(string blobName, CancellationToken cancel);

    Task<T?> GetAsync<T>(string blobName, CancellationToken cancel);

    Task<object?> GetAsync(Type modelType, string blobName, CancellationToken cancel);

    Task<List<T>> GetAllAsync<T>(string prefix, Func<string, bool> filter, CancellationToken cancellationToken);

    Task GetAllAsync(
        Type modelType,
        string prefix,
        Func<string, bool> filter,
        Func<IList<object>, CancellationToken, Task> onBatchReceived,
        int batchSize = 100,
        CancellationToken cancellationToken = default);

    Task<string> UploadAsync(string blobFolder, string blobName, string blobContent, string? etag = null, CancellationToken cancellationToken = default);

    Task UploadBatchAsync<T>(string blobFolder, Func<T, string> getName, IList<T> list,
        CancellationToken cancellationToken);

    Task<string> UpsertAsync(string blobName, byte[] content, string? etag = null, CancellationToken cancellationToken = default);

    Task<string> DownloadAsync(string blobFolder, string blobName, string localFolder, CancellationToken cancellationToken);

    Task<long> CountAsync<T>(string prefix, Func<T, bool> filter, CancellationToken cancellationToken);

    Task<long> CountAsync(string containerFilter, string blobFilter, CancellationToken cancel);

    Task<long> CountAsync(string prefix, CancellationToken cancel);

    Task<IList<T>> TryAcquireLeaseAsync<T>(string blobFolder, int take, Action<T> update, TimeSpan timeout);

    Task ReleaseLeaseAsync(string blobName);

    Task DeleteBlobsAsync(string blobFolder, CancellationToken cancellationToken);

    Task DeleteAsync(string blobName, CancellationToken token);

    Task<BlobInfo?> GetBlobInfoAsync(string blobName, CancellationToken cancel);

    Task<DateTime> GetLastModificationTimeAsync(CancellationToken cancel);
}