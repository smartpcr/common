// -----------------------------------------------------------------------
// <copyright file="Logger.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Tests.Hooks;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

internal static partial class Logger
{
    [LoggerMessage(
        0,
        LogLevel.Warning,
        "Starting initializer, env name = {envName}..." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void StartingInitializer(
        this ILogger logger,
        string envName = "",
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        1,
        LogLevel.Warning,
        "Func initializer finished its' work." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void FinishedInitialization(
        this ILogger logger,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);
}