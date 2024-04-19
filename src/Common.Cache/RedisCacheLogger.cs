// -----------------------------------------------------------------------
// <copyright file="RedisCacheLogger.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

internal static partial class RedisCacheLogger
{
    [LoggerMessage(
        0,
        LogLevel.Information,
        "retrieving {secretName} for redis connection, key={key}, host={host}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void BuildRedisConnection(
        this ILogger logger,
        string secretName,
        string key,
        string host,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);
}