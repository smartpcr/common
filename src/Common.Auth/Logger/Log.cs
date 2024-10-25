// -----------------------------------------------------------------------
// <copyright file="Log.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Auth.Logger;

using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

internal static partial class Log
{
    [LoggerMessage(
        1,
        LogLevel.Information,
        "Starting to get access token, scenario: {scenarios}, client secret source: {clientSecretSource}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void GettingAccessToken(
        this ILogger logger,
        AadAuthScenarios scenarios,
        AadClientSecretSource clientSecretSource,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        2,
        LogLevel.Information,
        "Got access token, scopes: {scopes}, access token: obfuscated, expires on: {expiresOn}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void GotAccessToken(
        this ILogger logger,
        string[] scopes,
        DateTimeOffset expiresOn,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        3,
        LogLevel.Error,
        "An exception was thrown while getting access token: {ex}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void FailedToGetAccessToken(
        this ILogger logger,
        Exception ex,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);
}