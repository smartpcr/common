// -----------------------------------------------------------------------
// <copyright file="QueueStorageClientLogger.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Storage.Queues;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

internal static partial class QueueStorageClientLogger
{
    [LoggerMessage(
        1,
        LogLevel.Information,
        "creating queue client for type {typeName}..." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void CreatingQueueClient(
        this ILogger logger,
        string typeName,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        2,
        LogLevel.Information,
        "creating queue: account={account}, authMode={authMode}, typeName={typeName}..." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void CreateQueueWithTypeClientStart(
        this ILogger logger,
        string account,
        string authMode,
        string typeName,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        3,
        LogLevel.Information,
        "total of {messageCount} messages found from queue {queueName}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void DequeueStop(
        this ILogger logger,
        int messageCount,
        string queueName,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        4,
        LogLevel.Warning,
        "message exceed retry count {retryCount} is over {maxRetryCount}, moved to dead letter queue: {deadLetterQueueName}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void MoveToDeadLetterQueue(
        this ILogger logger,
        string deadLetterQueueName,
        int retryCount,
        int maxRetryCount,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        5,
        LogLevel.Error,
        "queue {queueName} not found, error: {error}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void QueueNotFoundError(
        this ILogger logger,
        string queueName,
        string error,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        6,
        LogLevel.Error,
        "Field to connect to queue {queueName}, error: {error}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ConnectToQueueError(
        this ILogger logger,
        string queueName,
        string error,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);
}