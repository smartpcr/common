// -----------------------------------------------------------------------
// <copyright file="DataReaderLogger.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Shared;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

internal static partial class DataReaderLogger
{
    [LoggerMessage(
        1,
        LogLevel.Error,
        "Failed to read enum {enumType} with value {enumValue}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ReadEnumError(
        this ILogger logger,
        string enumType,
        string enumValue,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);
}