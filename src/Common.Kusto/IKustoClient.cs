// -----------------------------------------------------------------------
// <copyright file="IKustoClient.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Kusto;

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

public interface IKustoClient : IDisposable
{
    Task<IEnumerable<T>> ExecuteQuery<T>(string query, TimeSpan timeout = default, CancellationToken cancellationToken = default);

    Task<(int Total, T? LastRecord)> ExecuteQuery<T>(
        string query,
        Func<IList<T>, CancellationToken, Task> onBatchReceived,
        CancellationToken cancellationToken = default,
        int batchSize = 100);

    Task<(int Total, object? LastRecord)> ExecuteQuery(
        Type entityType,
        string query,
        Func<IList<object>, CancellationToken, Task> onBatchReceived,
        CancellationToken cancellationToken = default,
        int batchSize = 100);

    Task<IEnumerable<T>> ExecuteFunction<T>(
        string functionName,
        CancellationToken cancellationToken,
        params (string name, string value)[] parameters);

    Task ExecuteFunction<T>(
        string functionName,
        (string name, string value)[] parameters,
        Func<IList<T>, CancellationToken, Task> onBatchReceived,
        CancellationToken cancellationToken = default,
        int batchSize = 100);

    Task<int> BulkInsert<T>(string tableName, IList<T> items, IngestMode ingestMode, string idPropName, CancellationToken cancellationToken);

    Task<T?> ExecuteScalar<T>(string query, string fieldName, CancellationToken cancel);

    Task<IDataReader> ExecuteReader(string query);

    Task EnsureTable<T>(string tableName);

    Task DropTable(string tableName, CancellationToken cancel);

    /// <summary>
    /// recreate staging table with schema copied from target table
    /// </summary>
    /// <param name="targetTableName"></param>
    /// <param name="stagingTableName"></param>
    /// <param name="ingestionMapName"></param>
    /// <param name="cancel"></param>
    Task RefreshStagingTable(string targetTableName, string stagingTableName, string ingestionMapName, CancellationToken cancel);

    Task SwapTable(string targetTable, string stagingTable);

    Task CopyRetentionPolicy(string fromTableName, string toTableName);

    #region schema

    Task<IEnumerable<KustoTable>> ListTables();

    Task<IEnumerable<KustoFunction>> ListFunctions();

    Task<long> GetTableRecordCount(string tableName, CancellationToken cancel);

    Task<(DateTime ingestionTime, long count)> GetLastIngestionTimeAndCount(string tableName, CancellationToken cancel);

    #endregion
}