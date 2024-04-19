// -----------------------------------------------------------------------
// <copyright file="KustoClientLogger.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Kusto;

using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

internal static partial class KustoClientLogger
{
    [LoggerMessage(
        0,
        LogLevel.Debug,
        "Executing query: {query}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ExecuteQueryStart(
        this ILogger logger,
        string query,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        1,
        LogLevel.Information,
        "It took {elapsedMilliseconds} ms to query {count} records. Query: {query}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ExecuteQueryStop(
        this ILogger logger,
        string query,
        int count,
        long elapsedMilliseconds,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        2,
        LogLevel.Debug,
        "Bulk inserting {count} records into table {tableName}..." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void BulkInsertStart(
        this ILogger logger,
        int count,
        string tableName,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        3,
        LogLevel.Information,
        "It took {elapsedMilliseconds} ms to bulk insert {count} records into table {tableName}, total size {totalSize} bytes." +
        ", \n\tcalled from {memberName}, in file {callerFile}")]
    public static partial void BulkInsertStop(
        this ILogger logger,
        int count,
        string tableName,
        long elapsedMilliseconds,
        long totalSize,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "");

    [LoggerMessage(
        4,
        LogLevel.Debug,
        "Executing scalar query: {query}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ExecuteScalarStart(
        this ILogger logger,
        string query,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        5,
        LogLevel.Information,
        "It took {elapsedMilliseconds} ms to execute scalar query: {query}, result: {result}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ExecuteScalarStop(
        this ILogger logger,
        string query,
        long elapsedMilliseconds,
        string result,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        6,
        LogLevel.Error,
        "Failed to execute scalar query: {query} in {elapsedMilliseconds} ms, error: {error}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ExecuteScalarError(
        this ILogger logger,
        string query,
        long elapsedMilliseconds,
        string error,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        7,
        LogLevel.Error,
        "Executing reader with query: {query}..." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ExecuteReaderStart(
        this ILogger logger,
        string query,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        8,
        LogLevel.Error,
        "Execute reader with query: {query} in {elapsedMilliseconds} ms" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ExecuteReaderStop(
        this ILogger logger,
        string query,
        long elapsedMilliseconds,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        9,
        LogLevel.Error,
        "Dropping table {tableName}..." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void DropTableStart(
        this ILogger logger,
        string tableName,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        10,
        LogLevel.Error,
        "Dropped table {tableName} in {elapsedMilliseconds} ms" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void DropTableStop(
        this ILogger logger,
        string tableName,
        long elapsedMilliseconds,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        11,
        LogLevel.Information,
        "Refreshing staging table {stageTableName} to target table {targetTableName} using ingestion map {ingestionMapName}..." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void RefreshStagingTableStart(
        this ILogger logger,
        string stageTableName,
        string targetTableName,
        string ingestionMapName,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        12,
        LogLevel.Information,
        "Refreshed staging table {stageTableName} to target table {targetTableName} using ingestion map {ingestionMapName} in {elapsedMilliseconds} ms" +
        ", \n\tcalled from {memberName}, in file {callerFile}")]
    public static partial void RefreshStagingTableStop(
        this ILogger logger,
        string stageTableName,
        string targetTableName,
        string ingestionMapName,
        long elapsedMilliseconds,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "");

    [LoggerMessage(
        13,
        LogLevel.Information,
        "Swapping table between {fromTableName} and {toTableName}..." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void SwapTableStart(
        this ILogger logger,
        string fromTableName,
        string toTableName,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        14,
        LogLevel.Information,
        "Swapped table between {fromTableName} and {toTableName} in {elapsedMilliseconds} ms" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void SwapTableStop(
        this ILogger logger,
        string fromTableName,
        string toTableName,
        long elapsedMilliseconds,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        15,
        LogLevel.Information,
        "Copying retention policy from {sourceTableName} to {targetTableName}..." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void CopyRetentionPolicyStart(
        this ILogger logger,
        string sourceTableName,
        string targetTableName,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        16,
        LogLevel.Information,
        "Copied retention policy from {sourceTableName} to {targetTableName} in {elapsedMilliseconds} ms" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void CopyRetentionPolicyStop(
        this ILogger logger,
        string sourceTableName,
        string targetTableName,
        long elapsedMilliseconds,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        17,
        LogLevel.Information,
        "Listing tables..." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ListTablesStart(
        this ILogger logger,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        18,
        LogLevel.Information,
        "Listed {count} tables in {elapsedMilliseconds} ms" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ListTablesStop(
        this ILogger logger,
        int count,
        long elapsedMilliseconds,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        19,
        LogLevel.Information,
        "Listing functions..." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ListFunctionsStart(
        this ILogger logger,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        20,
        LogLevel.Information,
        "Listed {count} functions in {elapsedMilliseconds} ms" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ListFunctionsStop(
        this ILogger logger,
        int count,
        long elapsedMilliseconds,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        21,
        LogLevel.Information,
        "Record count in table {tableName} is {count}." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void GetRecordCountStop(
        this ILogger logger,
        long count,
        string tableName,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        22,
        LogLevel.Information,
        "Last ingestion time is {lastIngestionTime}, record count is {count}." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void GetLastIngestionTimeAndCount(
        this ILogger logger,
        DateTime lastIngestionTime,
        long count,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        23,
        LogLevel.Warning,
        "Table {tableName} does not exist." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void TableNotExists(
        this ILogger logger,
        string tableName,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        24,
        LogLevel.Debug,
        "alter retention command: {command}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void AlterRetentionCommand(
        this ILogger logger,
        string command,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        25,
        LogLevel.Debug,
        "reading schema for table {tableName}..." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ReadTableSchema(
        this ILogger logger,
        string tableName,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        26,
        LogLevel.Debug,
        "reading schema for function {functionName}..." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ReadFunctionSchema(
        this ILogger logger,
        string functionName,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        27,
        LogLevel.Information,
        "Creating table {tableName} with {columnCount} columns..." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void CreateTable(
        this ILogger logger,
        string tableName,
        int columnCount,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        28,
        LogLevel.Information,
        "Id query: {query}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void IdQuery(
        this ILogger logger,
        string query,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        29,
        LogLevel.Information,
        "It took {elapsedMilliseconds} ms to read {count} records from reader." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ReadReaderStop(
        this ILogger logger,
        int count,
        long elapsedMilliseconds,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        30,
        LogLevel.Information,
        "sending batch #{batchCount}, total: {total} records..." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void OnBatchReceived(
        this ILogger logger,
        int batchCount,
        int total,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        31,
        LogLevel.Trace,
        "reading {total} records from kusto..." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ReadingRecords(
        this ILogger logger,
        int total,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        32,
        LogLevel.Warning,
        "Query cancelled." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void QueryCancelled(
        this ILogger logger,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        33,
        LogLevel.Information,
        "Total of {total} records retrieved in {elapsedMilliseconds} ms." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ReadRecordsStop(
        this ILogger logger,
        int total,
        long elapsedMilliseconds,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);
}