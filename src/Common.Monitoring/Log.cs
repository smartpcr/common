// -----------------------------------------------------------------------
// <copyright file="Log.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring;

using System.Runtime.CompilerServices;
using Logs;
using Microsoft.Extensions.Logging;

internal static partial class Log
{
    [LoggerMessage(
        1,
        LogLevel.Information,
        "Logger configured with settings: {LogSettings}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void LoggerConfigured(
        this ILogger logger,
        LogSettings logSettings,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);
}