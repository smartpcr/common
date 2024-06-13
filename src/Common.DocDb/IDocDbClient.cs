// -----------------------------------------------------------------------
// <copyright file="IDocDbClient.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.DocDb;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Shared;

/// <summary>
/// Represents a client to interact with a specific collection in a specific DocumentDb store
/// </summary>
public interface IDocDbClient : IDisposable
{
    CosmosClient Client { get; }

    /// <summary>
    /// Gets collection, container is initialized when accessed first time, throw exception if not exists
    /// </summary>
    Container Collection { get; }

    /// <summary>
    /// switch context to different collection, throw exception if not exists
    /// </summary>
    /// <param name="collectionName">collection name</param>
    void SwitchCollection(string collectionName);

    /// <summary>
    /// Count docs across partitions
    /// </summary>
    /// <param name="whereClause">filter</param>
    /// <param name="cancel">cancel token</param>
    /// <returns>document count</returns>
    Task<int> CountAsync(string? whereClause, CancellationToken cancel = default);

    /// <summary>
    /// Creates a new object in the specific DocumentDb store.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="object">The object to create.</param>
    /// <param name="cancel">The cancellation token.</param>
    /// <returns>The ID of the created object.</returns>
    Task<string> CreateObjectAsync<T>(T @object, CancellationToken cancel = default) where T : IBaseEntity;

    /// <summary>
    /// Update (if exists) or insert (if it doesn't exist) an object to the store.
    /// New objects will automatically receive a system-generated id.
    /// </summary>
    /// <param name="object">The object being stored.</param>
    /// <param name="requestOptions">The request options.</param>
    /// <param name="cancel">The cancel token.</param>
    /// <typeparam name="T">The object type</typeparam>
    /// <returns>THe system-generated id for this object.</returns>
    Task<string> UpsertObjectAsync<T>(T @object, ItemRequestOptions? requestOptions = null, CancellationToken cancel = default) where T : IBaseEntity;

    /// <summary>
    /// Replaces an object in the DocumentDb store.
    /// If the object exists, it will be replaced with the provided object.
    /// If the object does not exist, it will be inserted.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="object">The object to be replaced or inserted.</param>
    /// <param name="cancel">The cancellation token.</param>
    /// <returns>The replaced or inserted object.</returns>
    Task<T> ReplaceObjectAsync<T>(T @object, CancellationToken cancel = default) where T : IBaseEntity;

    /// <summary>
    /// Update (if exists) or insert (if it doesn't exist) a list of objects to the store using bulk executor..
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="list">The list of objects being stored.</param>
    /// <param name="cancel">The cancel token.</param>
    /// <returns>The number of objects upserted.</returns>
    Task<int> UpsertObjectsAsync<T>(List<T> list, CancellationToken cancel = default) where T : IBaseEntity;

    /// <summary>
    /// Executes a SQL query against the specified collection in the DocumentDb store.
    /// </summary>
    /// <typeparam name="T">The type of objects returned by the query.</typeparam>
    /// <param name="querySpec">The SQL query specification.</param>
    /// <param name="cancel">The cancellation token.</param>
    /// <returns>A collection of objects that match the query.</returns>
    Task<IEnumerable<T>> QueryAsync<T>(QueryDefinition querySpec, CancellationToken cancel = default);

    /// <summary>
    /// Executes a query against the collection in batches, returning a paged result.
    /// </summary>
    /// <typeparam name="T">The type of objects in the result.</typeparam>
    /// <param name="querySpec">The query specification.</param>
    /// <param name="batchSize">Batch size, max item to return in each query.</param>
    /// <param name="cancel">The cancellation token.</param>
    /// <returns>A FeedResponse object representing the paged query result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when querySpec is null.</exception>
    /// <exception cref="TaskCanceledException">Thrown when the operation is canceled.</exception>
    /// <exception cref="DocumentClientException">Thrown when the query execution fails.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the collection or connection is invalid or disposed.</exception>
    Task<IEnumerable<T>> QueryInBatchesAsync<T>(QueryDefinition querySpec, int batchSize = 1000, CancellationToken cancel = default);

    /// <summary>
    /// Deletes an object from the DocumentDb store.
    /// </summary>
    /// <typeparam name="T">The type of objects in the result.</typeparam>
    /// <param name="id">The id of the object to delete.</param>
    /// <param name="partitionKey">The partition key value of object.</param>
    /// <param name="cancel">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// The object will be deleted from the collection in the DocumentDb store
    /// based on the given id. If the object does not exist, no action will be taken.
    /// </remarks>
    Task DeleteObjectAsync<T>(string id, string partitionKey, CancellationToken cancel = default) where T : IBaseEntity;

    /// <summary>
    /// Deletes documents from the collection based on a specified query.
    /// </summary>
    /// <typeparam name="T">The type of objects in the result.</typeparam>
    /// <param name="query">The query string.</param>
    /// <param name="cancel">The cancellation token.</param>
    /// <returns>The number of documents deleted.</returns>
    Task<int> DeleteByQueryAsync<T>(string query, CancellationToken cancel = default) where T : IBaseEntity;

    /// <summary>
    /// Reads an object with the specified ID from the document store.
    /// </summary>
    /// <typeparam name="T">The type of the object to read.</typeparam>
    /// <param name="id">The ID of the object to read.</param>
    /// <param name="partitionKey">The partition key value of object.</param>
    /// <param name="cancel">The cancellation token.</param>
    /// <returns>The object with the specified ID.</returns>
    Task<T?> ReadObjectAsync<T>(string id, string partitionKey, CancellationToken cancel = default);

    /// <summary>
    /// Clears all documents in the collection.
    /// </summary>
    /// <param name="cancel">The cancellation token.</param>
    /// <returns>The number of documents cleared.</returns>
    Task<int> ClearAllAsync(CancellationToken cancel = default);

    /// <summary>
    /// Executes a stored procedure in the DocumentDb store.
    /// </summary>
    /// <typeparam name="T">The type of the result returned by the stored procedure.</typeparam>
    /// <param name="storedProcName">The name of the stored procedure to execute.</param>
    /// <param name="partitionKey">The partition key value of object.</param>
    /// <param name="cancel">A cancellation token to cancel the operation.</param>
    /// <param name="paramValues">The parameter values to pass to the stored procedure.</param>
    /// <returns>The result returned by the stored procedure.</returns>
    Task<T> ExecuteStoredProcedureAsync<T>(string storedProcName, string partitionKey, CancellationToken cancel, params object[] paramValues);

    /// <summary>
    /// Retrieves the last modification time of a document based on the provided query.
    /// </summary>
    /// <param name="query">The query used to filter the documents.</param>
    /// <param name="cancel">The cancellation token.</param>
    /// <returns>The last modification time of the document.</returns>
    Task<DateTime> GetLastModificationTimeAsync(string query, CancellationToken cancel);

    /// <summary>
    /// Retrieves the counts of distinct values for a given field in the collection.
    /// </summary>
    /// <param name="fieldName">The name of the field to get the counts for.</param>
    /// <param name="query">Optional query to filter the documents.</param>
    /// <param name="cancel">Optional cancellation token.</param>
    /// <returns>A collection of distinct field values and their corresponding counts.</returns>
    Task<IEnumerable<string>> GetDistinctValuesByFieldAsync(string fieldName, string? query = null, CancellationToken cancel = default);

    /// <summary>
    /// Counts the number of documents in the collection that have a specific field value matching a given query.
    /// </summary>
    /// <param name="fieldName">The name of the field to count.</param>
    /// <param name="query">The query to filter the documents. Optional.</param>
    /// <param name="cancel">The cancellation token. Optional.</param>
    /// <returns>The count of documents.</returns>
    Task<long> CountByFieldAsync(string fieldName, string? query = null, CancellationToken cancel = default);

    /// <summary>
    /// Retrieves id mappings for a given field name.
    /// </summary>
    /// <param name="fieldName">The name of the field to retrieve id mappings for.</param>
    /// <param name="cancel">Cancellation token to cancel the operation.</param>
    /// <returns>A dictionary containing the id mappings.</returns>
    Task<Dictionary<string, string>> GetIdMappingsAsync(string fieldName, CancellationToken cancel);

    /// <summary>
    /// Executes the specified query and returns the scalar value of the specified field.
    /// </summary>
    /// <typeparam name="T">The type of the scalar value.</typeparam>
    /// <param name="query">The query to execute.</param>
    /// <param name="fieldName">The name of the field whose scalar value is to be returned.</param>
    /// <param name="cancel">The cancellation token.</param>
    /// <returns>The scalar value of the specified field.</returns>
    Task<T?> ExecuteScalarAsync<T>(string query, string fieldName, CancellationToken cancel);

    /// <summary>
    /// Executes a query and processes the result in batches.
    /// </summary>
    /// <typeparam name="T">The type of the result object.</typeparam>
    /// <param name="query">The query to execute.</param>
    /// <param name="onBatchReceived">The callback function to process each batch of results.</param>
    /// <param name="batchSize">The number of items in each result batch.</param>
    /// <param name="cancel">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteQueryAsync<T>(
        string query,
        Func<IList<T>, CancellationToken, Task> onBatchReceived,
        int batchSize = 100,
        CancellationToken cancel = default);

    /// <summary>
    /// Retrieves all documents from the specified collection.
    /// </summary>
    /// <typeparam name="T">The type of the documents.</typeparam>
    /// <returns>An IQueryable collection of documents.</returns>
    IQueryable<T> GetDocuments<T>();
}
