// -----------------------------------------------------------------------
// <copyright file="QueueClientAuthHelperLogger.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Storage.Queues;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

internal static partial class QueueClientAuthHelperLogger
{
    [LoggerMessage(
        0,
        LogLevel.Information,
        "Creating queue client for account {account} using {authMode} authentication." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void CreateQueueClientStart(
        this ILogger logger,
        string account,
        string authMode,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        1,
        LogLevel.Information,
        "Succeed to access queue: account={account} using {authMode} authentication." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void CreateQueueClientStop(
        this ILogger logger,
        string account,
        string authMode,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        2,
        LogLevel.Error,
        "Failed to access queue: account={account} using {authMode} authentication, error={error}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void CreateQueueClientFailed(
        this ILogger logger,
        string account,
        string authMode,
        string error,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        3,
        LogLevel.Warning,
        "Queue {queueName} not found in account {account}." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void QueueNotFound(
        this ILogger logger,
        string account,
        string queueName,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        4,
        LogLevel.Information,
        "Queue {queueName} has {queueLength} messages." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ReportQueueLength(
        this ILogger logger,
        string queueName,
        int queueLength,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);
}