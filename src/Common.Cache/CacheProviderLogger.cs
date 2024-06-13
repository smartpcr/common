// -----------------------------------------------------------------------
// <copyright file="CacheProviderLogger.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

internal static partial class CacheProviderLogger
{
    [LoggerMessage(
        0,
        LogLevel.Information,
        "updating cache {key}..." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void UpdateCacheStart(
        this ILogger logger,
        string key,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        1,
        LogLevel.Information,
        "clearing... {totalCleared} of {totalToClear}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ClearCache(
        this ILogger logger,
        int totalCleared,
        int totalToClear,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        2,
        LogLevel.Error,
        "failed to deserialize cached item for type {typeName}: {error}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void CacheItemDeserializeError(
        this ILogger logger,
        string typeName,
        string error,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);
}