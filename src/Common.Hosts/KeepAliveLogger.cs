// -----------------------------------------------------------------------
// <copyright file="KeepAliveLogger.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Hosts;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

internal static partial class KeepAliveLogger
{
    [LoggerMessage(
        0,
        LogLevel.Trace,
        "Heartbeat from KeepAlive" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void Heartbeat(
        this ILogger logger,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        1,
        LogLevel.Critical,
        "KeepAlive heartbeat stopped" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void HeatbeatStopped(
        this ILogger logger,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);
}