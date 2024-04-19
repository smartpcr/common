// -----------------------------------------------------------------------
// <copyright file="AuthLogger.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Auth;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

internal static partial class AuthLogger
{
    [LoggerMessage(
        0,
        LogLevel.Information,
        "Set a token in the the in-memory cache by key: {Key}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void CreateEntryInMemory(
        this ILogger logger,
        object key,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        1,
        LogLevel.Information,
        "Remove a token from the in-memory cache by key: {Key}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void RemoveEntryInMemory(
        this ILogger logger,
        object key,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(2, LogLevel.Information, "Get a token from the in-memory cache by key: {Key}")]
    public static partial void GetEntryInMemory(
        this ILogger logger,
        object key,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        3,
        LogLevel.Information,
        "Get a token from the distributed cache by key: {Key}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void GetTokenFromCache(
        this ILogger logger,
        string key,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        4,
        LogLevel.Information,
        "Remove a token from the distributed cache by key: {Key}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void RemoveTokenFromCache(
        this ILogger logger,
        string key,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        5,
        LogLevel.Information,
        "Set a token in the distributed cache by key: {Key}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void SetTokenInCache(
        this ILogger logger,
        string key,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);
}