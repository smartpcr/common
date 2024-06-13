// -----------------------------------------------------------------------
// <copyright file="DocDbClientAuthHelperLogger.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.DocDb;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

internal static partial class DocDbClientAuthHelperLogger
{
    [LoggerMessage(
        0,
        LogLevel.Information,
        "Creating cosmos client for account {account} using auth mode: {authMode}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void CreateCosmosClientUsing(
        this ILogger logger,
        string account,
        string authMode,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        1,
        LogLevel.Information,
        "Reading secret {secretName} from key vault {vaultName}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ReadSecretFromKeyVaultStart(
        this ILogger logger,
        string secretName,
        string vaultName,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        2,
        LogLevel.Information,
        "Retrieved secret {secretName} from key vault {vaultName}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ReadSecretFromKeyVaultStop(
        this ILogger logger,
        string secretName,
        string vaultName,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        3,
        LogLevel.Information,
        "Reading secret {secretName} from environment" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ReadSecretFromEnvironmentStart(
        this ILogger logger,
        string secretName,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        4,
        LogLevel.Information,
        "Retrieved secret {secretName} from environment" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ReadSecretFromEnvironmentStop(
        this ILogger logger,
        string secretName,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        5,
        LogLevel.Error,
        "Failed to read secret {secretName} from environment" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ReadSecretFromEnvironmentFailed(
        this ILogger logger,
        string secretName,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);
}