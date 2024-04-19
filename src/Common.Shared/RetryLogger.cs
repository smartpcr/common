// -----------------------------------------------------------------------
// <copyright file="RetryLogger.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Shared;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

internal static partial class RetryLogger
{
    [LoggerMessage(
        1,
        LogLevel.Error,
        "invoke failed at attempt={attempt}, will continue..., error: {errorMessage}, stackTrace: {stackTrace}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void RetryErrorContinue(
        this ILogger logger,
        int attempt,
        string errorMessage,
        string stackTrace,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        2,
        LogLevel.Error,
        "retry failed after {attempt} attempt, error: {errorMessage}, stackTrace: {stackTrace}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void RetryErrorStop(
        this ILogger logger,
        int attempt,
        string errorMessage,
        string stackTrace,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);
}