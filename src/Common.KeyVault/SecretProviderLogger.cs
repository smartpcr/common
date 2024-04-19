// -----------------------------------------------------------------------
// <copyright file="SecretProviderLogger.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.KeyVault;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

internal static partial class SecretProviderLogger
{
    [LoggerMessage(
        1,
        LogLevel.Information,
        "Reading secret {secretName} from key vault {vaultName} using auth type {vaultAuthType}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}",
        EventName = "ReadSecretFromKeyVaultStart")]
    public static partial void GetSecretStart(
        this ILogger logger,
        string secretName,
        string vaultName,
        string vaultAuthType,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        2,
        LogLevel.Information,
        "Reading secret {secretName} from key vault {vaultName} using auth type {vaultAuthType} completed in {elapsedMilliseconds} ms." +
        ", \n\tcalled from {memberName}, in file {callerFile}",
        EventName = "ReadSecretFromKeyVaultStop")]
    public static partial void GetSecretSucceed(
        this ILogger logger,
        string secretName,
        string vaultName,
        string vaultAuthType,
        long elapsedMilliseconds,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "");

    [LoggerMessage(
        3,
        LogLevel.Error,
        "Reading secret {secretName} from key vault {vaultName} using auth type {vaultAuthType} failed: {errorMessage}, duration: {elapsedMilliseconds} ms." +
        ", \n\tcalled from {memberName}",
        EventName = "ReadSecretFromKeyVaultFailed")]
    public static partial void GetSecretFailed(
        this ILogger logger,
        string secretName,
        string vaultName,
        string vaultAuthType,
        string errorMessage,
        long elapsedMilliseconds,
        [CallerMemberName] string memberName = "");

    [LoggerMessage(
        4,
        LogLevel.Information,
        "Reading certificate {certName} from key vault {vaultName} using auth type {vaultAuthType}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}",
        EventName = "ReadCertFromKeyVaultStart")]
    public static partial void GetCertStart(
        this ILogger logger,
        string certName,
        string vaultName,
        string vaultAuthType,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        5,
        LogLevel.Information,
        "Reading certificate {certName} from key vault {vaultName} using auth type {vaultAuthType} completed in {elapsedMilliseconds} ms." +
        ", \n\tcalled from {memberName}, in file {callerFile}",
        EventName = "ReadCertFromKeyVaultStop")]
    public static partial void GetCertSucceed(
        this ILogger logger,
        string certName,
        string vaultName,
        string vaultAuthType,
        long elapsedMilliseconds,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "");

    [LoggerMessage(
        6,
        LogLevel.Error,
        "Reading certificate {certName} from key vault {vaultName} using auth type {vaultAuthType} failed: {errorMessage}, duration: {elapsedMilliseconds} ms." +
        ", \n\tcalled from {memberName}",
        EventName = "ReadCertFromKeyVaultFailed")]
    public static partial void GetCertFailed(
        this ILogger logger,
        string certName,
        string vaultName,
        string vaultAuthType,
        string errorMessage,
        long elapsedMilliseconds,
        [CallerMemberName] string memberName = "");

    [LoggerMessage(
        7,
        LogLevel.Information,
        "Listing secret names from key vault {vaultName} using auth type {vaultAuthType}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}",
        EventName = "ListSecretNamesStart")]
    public static partial void ListSecretNamesStart(
        this ILogger logger,
        string vaultName,
        string vaultAuthType,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        8,
        LogLevel.Information,
        "Listing secret names from key vault {vaultName} using auth type {vaultAuthType} completed in {elapsedMilliseconds} ms. Found {secretCount} secrets." +
        ", \n\tcalled from {memberName}, in file {callerFile}",
        EventName = "ListSecretNamesStop")]
    public static partial void ListSecretNamesStop(
        this ILogger logger,
        string vaultName,
        string vaultAuthType,
        int secretCount,
        long elapsedMilliseconds,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "");

    [LoggerMessage(
        9,
        LogLevel.Error,
        "Listing secret names from key vault {vaultName} using auth type {vaultAuthType} failed: {errorMessage}, duration: {elapsedMilliseconds} ms." +
        ", \n\tcalled from {memberName}, in file {callerFile}",
        EventName = "ListSecretNamesFailed")]
    public static partial void ListSecretNamesFailed(
        this ILogger logger,
        string vaultName,
        string vaultAuthType,
        string errorMessage,
        long elapsedMilliseconds,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "");
}