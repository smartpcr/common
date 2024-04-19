// -----------------------------------------------------------------------
// <copyright file="DocDbClient.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.DocDb;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Config;
using EnsureThat;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Scripts;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using Shared;
using PartitionKey = Microsoft.Azure.Cosmos.PartitionKey;

public sealed class DocDbClient : IDocDbClient
{
    private const string BulkDeleteStoredProcedureName = "bulkDelete";
    private readonly IServiceProvider serviceProvider;
    private readonly ILoggerFactory loggerFactory;
    private readonly ILogger<DocDbClient> logger;
    private readonly Tracer tracer;
    private readonly DocDbSettings settings;

    public CosmosClient Client { get; }
    public Container Collection { get; private set; }

    public DocDbClient(
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory,
        IOptions<DocDbSettings>? docDbSettings = null)
    {
        this.serviceProvider = serviceProvider;
        this.loggerFactory = loggerFactory;
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        settings = docDbSettings?.Value ?? configuration.GetConfiguredSettings<DocDbSettings>();
        logger = loggerFactory.CreateLogger<DocDbClient>();
        var metadata = configuration.GetConfiguredSettings<ApplicationMetadata>();
        var traceProvider = serviceProvider.GetRequiredService<TracerProvider>();
        tracer = traceProvider.GetTracer(metadata.ApplicationName + $".{nameof(DocDbClient)}", metadata.BuildVersion);

        Client = InitDocumentClient();
        Collection = InitCollection(settings.Db, settings.Collection);
    }

    public void SwitchCollection(string collectionName)
    {
        using var _ = tracer.StartActiveSpan(nameof(SwitchCollection));
        if (Collection.Id == collectionName)
        {
            return;
        }

        Collection = InitCollection(settings.Db, collectionName);

        logger.SwitchCollection(collectionName);
    }

    public async Task<int> CountAsync(string? whereClause, CancellationToken cancel = default)
    {
        using var _ = tracer.StartActiveSpan(nameof(CountAsync));
        var countQuery = @"SELECT VALUE COUNT(1) FROM c";
        if (!string.IsNullOrEmpty(whereClause))
        {
            countQuery += $" where {whereClause}";
        }

        var count = 0;
        using var queryIterator = Collection.GetItemQueryIterator<int>(new QueryDefinition(countQuery));
        while (queryIterator.HasMoreResults)
        {
            var batchSize = await queryIterator.ReadNextAsync(cancel);
            count += batchSize.First();
        }

        return count;
    }

    public async Task<string> CreateObjectAsync<T>(T @object, CancellationToken cancel = default) where T : IBaseEntity
    {
        using var span = tracer.StartActiveSpan(nameof(UpsertObjectAsync));
        try
        {
            var requestOptions = new ItemRequestOptions();
            var response = await Collection.CreateItemAsync(@object, new PartitionKey(@object.GetPartitionKeyValue()), requestOptions, cancel);
            return response.Resource.Id;
        }
        catch (Exception ex)
        {
            logger.UpsertError(@object.Id, Collection.Id, ex.Message);
            span.SetStatus(Status.Error);
            throw;
        }
    }

    public async Task<string> UpsertObjectAsync<T>(
        T @object,
        ItemRequestOptions? requestOptions = null,
        CancellationToken cancel = default) where T : IBaseEntity
    {
        using var span = tracer.StartActiveSpan(nameof(UpsertObjectAsync));
        try
        {
            requestOptions ??= new ItemRequestOptions();
            if (!string.IsNullOrEmpty(@object.ETag))
            {
                requestOptions.IfMatchEtag = @object.ETag;
            }

            var response = await Collection.UpsertItemAsync(@object, new PartitionKey(@object.GetPartitionKeyValue()), requestOptions, cancel);
            return response.Resource.Id;
        }
        catch (Exception ex)
        {
            logger.UpsertError(@object.Id, Collection.Id, ex.Message);
            span.SetStatus(Status.Error);
            throw;
        }
    }

    public async Task<T> ReplaceObjectAsync<T>(T @object, CancellationToken cancel = default) where T : IBaseEntity
    {
        using var span = tracer.StartActiveSpan(nameof(ReplaceObjectAsync));
        try
        {
            var requestOptions = new ItemRequestOptions();
            if (!string.IsNullOrEmpty(@object.ETag))
            {
                requestOptions.IfMatchEtag = @object.ETag;
            }

            var response = await Collection.ReplaceItemAsync(@object, @object.Id, new PartitionKey(@object.GetPartitionKeyValue()), requestOptions, cancel);
            await using var ms = new MemoryStream();
            using var reader = new StreamReader(ms);
            return response.Resource;
        }
        catch (Exception ex)
        {
            logger.ReplaceError(@object.Id, Collection.Id, ex.Message);
            span.SetStatus(Status.Error);
            throw;
        }
    }

    public async Task<int> UpsertObjectsAsync<T>(List<T> list, CancellationToken cancel = default) where T : IBaseEntity
    {
        using var _ = tracer.StartActiveSpan(nameof(UpsertObjectsAsync));

        var stopwatch = Stopwatch.StartNew();
        var bulkOperations = new BulkOperations<T>(list.Count);

        foreach (var itemToCreate in list.Where(item => string.IsNullOrEmpty(item.Id)))
        {
            var task = Collection.CreateItemAsync(itemToCreate, new PartitionKey(itemToCreate.GetPartitionKeyValue()), cancellationToken: cancel);
            bulkOperations.Tasks.Add(CaptureOperationResponseAsync(task, itemToCreate));
        }

        foreach (var itemToUpdate in list.Where(item => !string.IsNullOrEmpty(item.Id)))
        {
            var task = Collection.ReplaceItemAsync(itemToUpdate, itemToUpdate.Id, new PartitionKey(itemToUpdate.GetPartitionKeyValue()), cancellationToken: cancel);
            bulkOperations.Tasks.Add(CaptureOperationResponseAsync(task, itemToUpdate));
        }

        // wait for all tasks to complete
        BulkOperationResponse<T> bulkOperationResponse = await bulkOperations.ExecuteAsync();
        logger.UpsertObjectsStop(list.Count, stopwatch.ElapsedMilliseconds);
        logger.ReportRequestUnitsConsumption(bulkOperationResponse.TotalRequestUnitsConsumed);
        logger.BulkInsertStop(bulkOperationResponse.SuccessfulDocuments);
        logger.BulkInsertFailed(bulkOperationResponse.Failures.Count);
        if (bulkOperationResponse.Failures.Count > 0)
        {
            logger.BulkInsertionFailure(bulkOperationResponse.Failures[0].Item1.Id, bulkOperationResponse.Failures[0].Item2.Message);
        }

        return bulkOperationResponse.SuccessfulDocuments;
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(QueryDefinition querySpec, CancellationToken cancel = default)
    {
        Ensure.That(querySpec).IsNotNull();
        using var span = tracer.StartActiveSpan(nameof(QueryAsync));
        try
        {
            var output = new List<T>();
            using var queryIterator = Collection.GetItemQueryIterator<T>(querySpec);
            while (queryIterator.HasMoreResults)
            {
                var response = await queryIterator.ReadNextAsync(cancel);
                output.AddRange(response);
            }

            return output;
        }
        catch (Exception ex)
        {
            logger.QueryError(querySpec.QueryText, Collection.Id, ex.Message);
            span.SetStatus(Status.Error);
            throw;
        }
    }

    public async Task<IEnumerable<T>> QueryInBatchesAsync<T>(
        QueryDefinition querySpec,
        int batchSize = 1000,
        CancellationToken cancel = default)
    {
        Ensure.That(querySpec).IsNotNull();
        using var span = tracer.StartActiveSpan(nameof(QueryInBatchesAsync));
        try
        {
            var output = new List<T>();
            var continuationToken = default(string);
            var batchReadCount = 0;
            using var queryIterator = Collection.GetItemQueryIterator<T>(
                querySpec,
                requestOptions: new QueryRequestOptions
                {
                    MaxItemCount = batchSize,
                },
                continuationToken: continuationToken);
            while (queryIterator.HasMoreResults)
            {
                var response = await queryIterator.ReadNextAsync(cancel);
                output.AddRange(response);
                continuationToken = response.ContinuationToken;
                batchReadCount = response.Count;
            }

            while (!string.IsNullOrEmpty(continuationToken) && batchReadCount > 0)
            {
                using var queryIterator2 = Collection.GetItemQueryIterator<T>(
                    querySpec,
                    requestOptions: new QueryRequestOptions
                    {
                        MaxItemCount = batchSize,
                    },
                    continuationToken: continuationToken);
                while (queryIterator2.HasMoreResults)
                {
                    var response = await queryIterator2.ReadNextAsync(cancel);
                    output.AddRange(response);
                    continuationToken = response.ContinuationToken;
                    batchReadCount = response.Count;
                }
            }

            return output;
        }
        catch (Exception ex)
        {
            logger.QueryError(querySpec.QueryText, Collection.Id, ex.Message);
            span.SetStatus(Status.Error);
            throw;
        }
    }

    public async Task DeleteObjectAsync<T>(string id, string partitionKey, CancellationToken cancel = default) where T : IBaseEntity
    {
        using var span = tracer.StartActiveSpan(nameof(DeleteObjectAsync));
        try
        {
            var existing = await ReadObjectAsync<T>(id, partitionKey, cancel);
            if (existing == null)
            {
                logger.LogWarning($"Unable to delete object. CollectionUrl={Collection.Id}, Id={id} not found");
                return;
            }

            await Collection.DeleteItemAsync<T>(id, new PartitionKey(existing.GetPartitionKeyValue()), cancellationToken: cancel);
        }
        catch (Exception ex)
        {
            logger.DeleteError(id, Collection.Id, ex.Message);
            span.SetStatus(Status.Error);
            throw;
        }
    }

    public async Task<int> DeleteByQueryAsync<T>(string query, CancellationToken cancel = default) where T : IBaseEntity
    {
        using var span = tracer.StartActiveSpan(nameof(DeleteByQueryAsync));
        try
        {
            var output = new List<T>();
            using var queryIterator = Collection.GetItemQueryIterator<T>(new QueryDefinition(query));
            while (queryIterator.HasMoreResults)
            {
                var response = await queryIterator.ReadNextAsync(cancel);
                output.AddRange(response);
            }

            var bulkOperations = new BulkOperations<T>(output.Count);
            foreach (var itemToDelete in output)
            {
                var task = Collection.DeleteItemAsync<T>(itemToDelete.Id, new PartitionKey(itemToDelete.GetPartitionKeyValue()), cancellationToken: cancel);
                bulkOperations.Tasks.Add(CaptureOperationResponseAsync(task, itemToDelete));
            }

            BulkOperationResponse<T> bulkOperationResponse = await bulkOperations.ExecuteAsync();
            logger.BulkDeleteStop(bulkOperationResponse.SuccessfulDocuments);
            logger.ReportRequestUnitsConsumption(bulkOperationResponse.TotalRequestUnitsConsumed);
            logger.BulkDeleteFailed(bulkOperationResponse.Failures.Count);

            return bulkOperationResponse.SuccessfulDocuments;
        }
        catch (Exception ex)
        {
            logger.DeleteByQueryError(query, Collection.Id, ex.Message);
            span.SetStatus(Status.Error);
            throw;
        }
    }

    public async Task<T?> ReadObjectAsync<T>(string id, string partitionKey, CancellationToken cancel = default)
    {
        using var span = tracer.StartActiveSpan(nameof(ReadObjectAsync));
        try
        {
            var response = await Collection.ReadItemAsync<T>(id, new PartitionKey(partitionKey), cancellationToken: cancel);
            return response.Resource;
        }
        catch (Exception ex)
        {
            logger.ReadError(id, Collection.Id, ex.Message);
            span.SetStatus(Status.Error);
            throw;
        }
    }

    public async Task<int> ClearAllAsync(CancellationToken cancel = default)
    {
        using var span = tracer.StartActiveSpan(nameof(ClearAllAsync));
        try
        {
            var iterator = Collection.Scripts.GetStoredProcedureQueryIterator<StoredProcedureProperties>(
                queryDefinition: new QueryDefinition("SELECT * FROM s WHERE s.id = @storedProcedureId")
                    .WithParameter("@storedProcedureId", BulkDeleteStoredProcedureName));
            var found = false;
            while (iterator.HasMoreResults)
            {
                var response = iterator.ReadNextAsync(cancel).Result;
                if (response.Any(sp => sp.Id.Equals(BulkDeleteStoredProcedureName, StringComparison.OrdinalIgnoreCase)))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                var spDefinition = await File.ReadAllTextAsync($"{BulkDeleteStoredProcedureName}.js", cancel);
                await Collection.Scripts.CreateStoredProcedureAsync(new StoredProcedureProperties(BulkDeleteStoredProcedureName, spDefinition), cancellationToken: cancel);
            }

            var containerProps = await Collection.ReadContainerAsync(cancellationToken: cancel);
            var partitionKey = containerProps.Resource.PartitionKeyPath.TrimStart('/').Trim();
            using var partitionKeyIterator = Collection.GetItemQueryIterator<string>(
                new QueryDefinition($"SELECT DISTINCT VALUE c.{partitionKey} FROM c"));
            var partitionKeys = new HashSet<string>();
            while (partitionKeyIterator.HasMoreResults)
            {
                var response = await partitionKeyIterator.ReadNextAsync(cancel);
                partitionKeys.UnionWith(response);
            }

            logger.ReportPartitionKeyCount(partitionKeys.Count);

            var totalDeleted = 0;
            foreach (var partitionKeyValue in partitionKeys)
            {
                var removeByPartitionKeyResponse = await Collection.Scripts.ExecuteStoredProcedureAsync<int>(
                    BulkDeleteStoredProcedureName,
                    new PartitionKey(partitionKeyValue),
                    new dynamic[] { partitionKeyValue },
                    cancellationToken: cancel);
                totalDeleted += removeByPartitionKeyResponse.Resource;
            }

            logger.DeleteObjectsStop(totalDeleted);
            return totalDeleted;
        }
        catch (Exception ex)
        {
            logger.ClearAllError(Collection.Id, ex.Message);
            span.SetStatus(Status.Error);
            throw;
        }
    }

    public async Task<T> ExecuteStoredProcedureAsync<T>(string storedProcName, string partitionKey, CancellationToken cancel, params object[] paramValues)
    {
        using var span = tracer.StartActiveSpan(nameof(ExecuteStoredProcedureAsync));
        try
        {
            var response = await Collection.Scripts.ExecuteStoredProcedureAsync<T>(
                storedProcName,
                new PartitionKey(partitionKey),
                paramValues,
                cancellationToken: cancel);
            return response.Resource;
        }
        catch (Exception ex)
        {
            logger.ExecuteStoreProcedureError(storedProcName, Collection.Id, ex.Message);
            span.SetStatus(Status.Error);
            throw;
        }
    }

    public async Task<DateTime> GetLastModificationTimeAsync(string query, CancellationToken cancel)
    {
        using var span = tracer.StartActiveSpan(nameof(GetLastModificationTimeAsync));
        try
        {
            var sql = $"select top 1 value(c._ts) from c order by c._ts desc";
            if (!string.IsNullOrEmpty(query))
            {
                sql = $"select top 1 value(c._ts) from c where {query} order by c._ts desc";
            }

            using var queryIterator = Collection.GetItemQueryIterator<long>(
                new QueryDefinition(sql),
                requestOptions: new QueryRequestOptions
                {
                    MaxItemCount = 1
                });
            var response = await queryIterator.ReadNextAsync(cancel);
            var ts = response.FirstOrDefault();
            var timestamp = ts != 0 ? DateTimeOffset.FromUnixTimeSeconds(ts) : default;
            logger.ReportLastModificationTime(timestamp);

            return timestamp.DateTime;
        }
        catch (Exception ex)
        {
            logger.GetModificationTimeError(query, Collection.Id, ex.Message);
            span.SetStatus(Status.Error);
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetDistinctValuesByFieldAsync(string fieldName, string? query = null, CancellationToken cancel = default)
    {
        using var span = tracer.StartActiveSpan(nameof(GetDistinctValuesByFieldAsync));
        try
        {
            var sql = $"select distinct c.{fieldName} from c";
            if (!string.IsNullOrEmpty(query))
            {
                sql = $"select distinct c.{fieldName} from c where {query}";
            }

            logger.UseSql(sql);
            using var queryIterator = Collection.GetItemQueryIterator<string>(new QueryDefinition(sql));
            var output = new HashSet<string>();
            while (queryIterator.HasMoreResults)
            {
                var response = await queryIterator.ReadNextAsync(cancel);
                output.UnionWith(response);
            }

            return output;
        }
        catch (Exception ex)
        {
            logger.GetCountByFiledError(fieldName, Collection.Id, ex.Message);
            span.SetStatus(Status.Error);
            throw;
        }
    }

    public async Task<long> CountByFieldAsync(string fieldName, string? query = null, CancellationToken cancel = default)
    {
        using var span = tracer.StartActiveSpan(nameof(CountByFieldAsync));
        try
        {
            var distinctValues = await GetDistinctValuesByFieldAsync(fieldName, query, cancel);
            return distinctValues.Count();
        }
        catch (Exception ex)
        {
            logger.GetCountByFiledError(fieldName, Collection.Id, ex.Message);
            span.SetStatus(Status.Error);
            throw;
        }
    }

    public async Task<Dictionary<string, string>> GetIdMappingsAsync(string fieldName, CancellationToken cancel)
    {
        using var span = tracer.StartActiveSpan(nameof(GetIdMappingsAsync));
        try
        {
            var sql = $"select distinct c.id, c.{fieldName} from c";
            logger.UseSql(sql);
            using var queryIterator = Collection.GetItemQueryIterator<dynamic>(new QueryDefinition(sql));
            var output = new Dictionary<string, string>();
            while (queryIterator.HasMoreResults)
            {
                var response = await queryIterator.ReadNextAsync(cancel);
                foreach (var item in response)
                {
                    output.Add(item.id, item[fieldName]);
                }
            }

            return output;
        }
        catch (Exception ex)
        {
            logger.GetIdMappingError(fieldName, Collection.Id, ex.Message);
            span.SetStatus(Status.Error);
            throw;
        }
    }

    public async Task<T?> ExecuteScalarAsync<T>(string query, string fieldName, CancellationToken cancel)
    {
        using var span = tracer.StartActiveSpan(nameof(ExecuteScalarAsync));
        try
        {
            using var queryIterator = Collection.GetItemQueryIterator<T>(
                new QueryDefinition(query),
                requestOptions: new QueryRequestOptions
                {
                    MaxItemCount = 1
                });
            var response = await queryIterator.ReadNextAsync(cancel);
            var item = response.FirstOrDefault();
            return item;
        }
        catch (Exception e)
        {
            logger.ExecuteScalarError(fieldName, query, Collection.Id, e.Message);
            span.SetStatus(Status.Error);
            throw;
        }
    }

    public async Task ExecuteQueryAsync<T>(
        string query,
        Func<IList<T>, CancellationToken, Task> onBatchReceived,
        int batchSize = 100,
        CancellationToken cancel = default)
    {
        using var span = tracer.StartActiveSpan(nameof(ExecuteQueryAsync));
        try
        {
            var continuationToken = default(string);
            var batchReadCount = 0;
            using var queryIterator = Collection.GetItemQueryIterator<T>(
                new QueryDefinition(query),
                requestOptions: new QueryRequestOptions
                {
                    MaxItemCount = batchSize
                },
                continuationToken: continuationToken);
            while (queryIterator.HasMoreResults)
            {
                var response = await queryIterator.ReadNextAsync(cancel);
                await onBatchReceived(response.ToList(), cancel);
                continuationToken = response.ContinuationToken;
                batchReadCount = response.Count;
            }

            while (!string.IsNullOrEmpty(continuationToken) && batchReadCount > 0)
            {
                using var queryIterator2 = Collection.GetItemQueryIterator<T>(
                    new QueryDefinition(query),
                    requestOptions: new QueryRequestOptions
                    {
                        MaxItemCount = batchSize
                    },
                    continuationToken: continuationToken);
                while (queryIterator2.HasMoreResults)
                {
                    var response = await queryIterator2.ReadNextAsync(cancel);
                    await onBatchReceived(response.ToList(), cancel);
                    continuationToken = response.ContinuationToken;
                    batchReadCount = response.Count;
                }
            }
        }
        catch (Exception ex)
        {
            logger.ExecuteQueryError(query, Collection.Id, ex.Message);
            span.SetStatus(Status.Error);
            throw;
        }
    }

    public IQueryable<T> GetDocuments<T>()
    {
        using var span = tracer.StartActiveSpan(nameof(GetDocuments));
        try
        {
            return Collection.GetItemLinqQueryable<T>();
        }
        catch (Exception ex)
        {
            logger.GetDocumentsError(Collection.Id, ex.Message);
            span.SetStatus(Status.Error);
            throw;
        }
    }

    private static async Task<OperationResponse<T>> CaptureOperationResponseAsync<T>(Task<ItemResponse<T>> task, T item)
    {
        if (task == null)
        {
            throw new ArgumentNullException(nameof(task));
        }

        try
        {
            ItemResponse<T> response = await task;
            return new OperationResponse<T>
            {
                Item = response.Resource,
                IsSuccessful = true,
                RequestUnitsConsumed = task.Result.RequestCharge
            };
        }
        catch (Exception ex)
        {
            if (ex is CosmosException cosmosException)
            {
                return new OperationResponse<T>
                {
                    Item = item,
                    RequestUnitsConsumed = cosmosException.RequestCharge,
                    IsSuccessful = false,
                    CosmosException = cosmosException
                };
            }

            return new OperationResponse<T>
            {
                Item = item,
                IsSuccessful = false,
                CosmosException = ex
            };
        }
    }

    #region IDisposable Support

    private bool _isDisposed; // To detect redundant calls

    private void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                try
                {
                    Client.Dispose();
                }
                catch (Exception ex)
                {
                    logger.DisposeCosmosClientError(ex.Message);
                }
            }

            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
    }

    #endregion

    #region initialization

    private CosmosClient InitDocumentClient()
    {
        var authHelper = new DocDbClientAuthHelper(serviceProvider, loggerFactory, settings);
        return authHelper.GetClient();
    }

    private Container InitCollection(string dbName, string collectionName)
    {
        var authHelper = new DocDbClientAuthHelper(serviceProvider, loggerFactory, settings);
        var documentClient = authHelper.GetClient();
        var database = documentClient.GetDatabase(dbName);
        if (database == null)
        {
            throw new InvalidOperationException($"Database '{dbName}' not found");
        }

        var collection = database.GetContainer(collectionName);
        if (collection == null)
        {
            throw new InvalidOperationException($"Collection '{collectionName}' not found in {settings.Account}/{dbName}");
        }

        logger.Connected(settings.Account, dbName, collectionName);

        return collection;
    }
    #endregion

}