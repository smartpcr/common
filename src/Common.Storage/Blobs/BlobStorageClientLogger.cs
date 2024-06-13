// -----------------------------------------------------------------------
// <copyright file="BlobStorageClientLogger.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Storage.Blobs;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

internal static partial class BlobStorageClientLogger
{
    [LoggerMessage(
        0,
        LogLevel.Information,
        "Accessing Blob Storage Account {AccountName} with Container {ContainerName} using {AuthMode} authentication." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void AccessingBlob(
        this ILogger logger,
        string accountName,
        string containerName,
        string authMode,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        1,
        LogLevel.Information,
        "Downloading blob {BlobName}..." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void DownloadBlobStart(
        this ILogger logger,
        string blobName,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        2,
        LogLevel.Information,
        "Blob {BlobName} downloaded in {elapsedMilliseconds} ms." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void DownloadBlobStop(
        this ILogger logger,
        string blobName,
        long elapsedMilliseconds,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        3,
        LogLevel.Information,
        "Checking blob {BlobName} exists..." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void BlobExistsStart(
        this ILogger logger,
        string blobName,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        4,
        LogLevel.Information,
        "Blob {BlobName} exists: {Exists}. Completed in {elapsedMilliseconds} ms." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void BlobExistsStop(
        this ILogger logger,
        string blobName,
        bool exists,
        long elapsedMilliseconds,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        5,
        LogLevel.Information,
        "Getting Blob {BlobName}..." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void GetBlobStart(
        this ILogger logger,
        string blobName,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        6,
        LogLevel.Information,
        "Getting Blob {BlobName} completed in {elapsedMilliseconds} ms." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void GetBlobStop(
        this ILogger logger,
        string blobName,
        long elapsedMilliseconds,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        7,
        LogLevel.Information,
        "Uploading blob {BlobName} to folder {blobFolder}..." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void UploadBlobStart(
        this ILogger logger,
        string blobFolder,
        string blobName,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        8,
        LogLevel.Information,
        "Blob {BlobName} uploaded to folder {blobFolder} in {elapsedMilliseconds} ms." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void UploadBlobStop(
        this ILogger logger,
        string blobFolder,
        string blobName,
        long elapsedMilliseconds,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        9,
        LogLevel.Information,
        "uploading {count} files to folder {folder}..." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void BulkUploadStart(
        this ILogger logger,
        int count,
        string folder,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        10,
        LogLevel.Information,
        "uploaded {count} files to folder {folder} in {elapsedMilliseconds} ms." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void BulkUploadStop(
        this ILogger logger,
        int count,
        string folder,
        long elapsedMilliseconds,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        11,
        LogLevel.Information,
        "Upserting blob {blobName} with length {contentLength}..." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void UpsertStart(
        this ILogger logger,
        string blobName,
        int contentLength,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        12,
        LogLevel.Information,
        "Downloading blob {blobPath} to folder {folder}..." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void DownloadStart(
        this ILogger logger,
        string blobPath,
        string folder,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        13,
        LogLevel.Information,
        "Blob {blobName} downloaded to file {filePath} in {elapsedMilliseconds} ms." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void DownloadStop(
        this ILogger logger,
        string blobName,
        string filePath,
        long elapsedMilliseconds,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        14,
        LogLevel.Information,
        "Acquiring lease on folder {folder}..." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void AcquireLeaseStart(
        this ILogger logger,
        string folder,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        15,
        LogLevel.Information,
        "Lease rejected on folder {folder}." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void LeaseRejectedOnFolder(
        this ILogger logger,
        string folder,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        16,
        LogLevel.Information,
        "Lease rejected on blob {blobName}." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void LeaseRejected(
        this ILogger logger,
        string blobName,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);
}