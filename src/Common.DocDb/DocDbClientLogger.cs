// -----------------------------------------------------------------------
// <copyright file="DocDbClientLogger.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.DocDb;

using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

internal static partial class DocDbClientLogger
{
    [LoggerMessage(
        1,
        LogLevel.Information,
        "Switched to collection {collectionName}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void SwitchCollection(
        this ILogger logger,
        string collectionName,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        2,
        LogLevel.Information,
        "Upserted {count} objects in {elapsedMilliseconds} ms" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void UpsertObjectsStop(
        this ILogger logger,
        int count,
        long elapsedMilliseconds,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        3,
        LogLevel.Debug,
        "Consumed {ru} RUs in total" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ReportRequestUnitsConsumption(
        this ILogger logger,
        double ru,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        4,
        LogLevel.Information,
        "Bulk inserted {count} documents." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void BulkInsertStop(
        this ILogger logger,
        int count,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        5,
        LogLevel.Error,
        "Failed {count} documents during bulk insert." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void BulkInsertFailed(
        this ILogger logger,
        int count,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        6,
        LogLevel.Error,
        "First failure in bulk insert: id: {id}, error: {error}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void BulkInsertionFailure(
        this ILogger logger,
        string id,
        string error,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        7,
        LogLevel.Information,
        "Bulk deleted {count} documents." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void BulkDeleteStop(
        this ILogger logger,
        int count,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        8,
        LogLevel.Error,
        "Failed {count} documents during bulk delete." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void BulkDeleteFailed(
        this ILogger logger,
        int count,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        9,
        LogLevel.Information,
        "Found {count} partition keys." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ReportPartitionKeyCount(
        this ILogger logger,
        int count,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        10,
        LogLevel.Information,
        "Deleted {count} documents." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void DeleteObjectsStop(
        this ILogger logger,
        int count,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        11,
        LogLevel.Information,
        "last modification time: {lastModificationTime}." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ReportLastModificationTime(
        this ILogger logger,
        DateTimeOffset lastModificationTime,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        12,
        LogLevel.Debug,
        "Use sql: {sql}." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void UseSql(
        this ILogger logger,
        string sql,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        13,
        LogLevel.Information,
        "Connected to cosmosdb: account={account}, db={db}, collection={collection}." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void Connected(
        this ILogger logger,
        string account,
        string db,
        string collection,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        14,
        LogLevel.Error,
        "Unable to upsert object, id={id}, collection={collection}, error={error}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void UpsertError(
        this ILogger logger,
        string id,
        string collection,
        string error,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        15,
        LogLevel.Error,
        "Unable to query, query={query}, collection={collection}, error={error}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void QueryError(
        this ILogger logger,
        string query,
        string collection,
        string error,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        16,
        LogLevel.Error,
        "Unable to replace object, id={id}, collection={collection}, error={error}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ReplaceError(
        this ILogger logger,
        string id,
        string collection,
        string error,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        17,
        LogLevel.Error,
        "Unable to delete object, id={id}, collection={collection}, error={error}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void DeleteError(
        this ILogger logger,
        string id,
        string collection,
        string error,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        18,
        LogLevel.Error,
        "Unable to delete by query {query}, collection={collection}, error={error}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void DeleteByQueryError(
        this ILogger logger,
        string query,
        string collection,
        string error,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        19,
        LogLevel.Error,
        "Unable to read object, id={id}, collection={collection}, error={error}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ReadError(
        this ILogger logger,
        string id,
        string collection,
        string error,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        20,
        LogLevel.Error,
        "Unable to clear all, collection={collection}, error={error}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ClearAllError(
        this ILogger logger,
        string collection,
        string error,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        21,
        LogLevel.Error,
        "Unable to execute store procedure {storedProcedureName}, collection={collection}, error={error}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ExecuteStoreProcedureError(
        this ILogger logger,
        string storedProcedureName,
        string collection,
        string error,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        22,
        LogLevel.Error,
        "Unable to get modification time using query: {query}, collection={collection}, error={error}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void GetModificationTimeError(
        this ILogger logger,
        string query,
        string collection,
        string error,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        23,
        LogLevel.Error,
        "Unable to get count by field {fieldName}, collection={collection}, error={error}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void GetCountByFiledError(
        this ILogger logger,
        string fieldName,
        string collection,
        string error,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        24,
        LogLevel.Error,
        "Unable to get id mappings, field {fieldName}, collection={collection}, error={error}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void GetIdMappingError(
        this ILogger logger,
        string fieldName,
        string collection,
        string error,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        25,
        LogLevel.Error,
        "Unable to execute scalar, field {fieldName}, query={query}, collection={collection}, error={error}" +
        ", \n\tcalled from {memberName}, in file {callerFile}")]
    public static partial void ExecuteScalarError(
        this ILogger logger,
        string fieldName,
        string query,
        string collection,
        string error,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "");

    [LoggerMessage(
        26,
        LogLevel.Error,
        "Unable to execute query {query}, collection={collection}, error={error}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ExecuteQueryError(
        this ILogger logger,
        string query,
        string collection,
        string error,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        27,
        LogLevel.Error,
        "Unable to get documents from collection={collection}, error={error}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void GetDocumentsError(
        this ILogger logger,
        string collection,
        string error,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        28,
        LogLevel.Error,
        "Unable to dispose cosmos client, error={error}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void DisposeCosmosClientError(
        this ILogger logger,
        string error,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);
}