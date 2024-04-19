// -----------------------------------------------------------------------
// <copyright file="BlobCacheLogger.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

internal static partial class BlobCacheLogger
{
    [LoggerMessage(
        1,
        LogLevel.Warning,
        "Blob cache miss for key {key}." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void BlobCacheMiss(
        this ILogger logger,
        string key,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        2,
        LogLevel.Warning,
        "Blob cache expired for key {key}." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void BlobCacheExpired(
        this ILogger logger,
        string key,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        3,
        LogLevel.Information,
        "Blob cache downloaded for key {key}." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void BlobCacheDownloaded(
        this ILogger logger,
        string key,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);
}