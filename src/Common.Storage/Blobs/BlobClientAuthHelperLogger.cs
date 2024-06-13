// -----------------------------------------------------------------------
// <copyright file="BlobClientAuthHelperLogger.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Storage.Blobs;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

internal static partial class BlobClientAuthHelperLogger
{
    [LoggerMessage(
        0,
        LogLevel.Information,
        "Creating Blob Client for Account {account} using {authMode} authentication." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void CreateBlobClientStart(
        this ILogger logger,
        string account,
        string authMode,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        1,
        LogLevel.Information,
        "Succeed to access blob: Account={account} using {authMode} authentication." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void CreateBlobClientStop(
        this ILogger logger,
        string account,
        string authMode,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        2,
        LogLevel.Error,
        "Failed to access blob: Account={account} using {authMode} authentication, error={error}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void CreateBlobClientFailed(
        this ILogger logger,
        string account,
        string authMode,
        string error,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        3,
        LogLevel.Error,
        "Failed to access blob: Account={account}, Container={container} using {authMode} authentication, error={error}" +
        ", \n\tcalled from {memberName}, in file {callerFile}")]
    public static partial void AccessBlobError(
        this ILogger logger,
        string account,
        string container,
        string authMode,
        string error,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "");
}