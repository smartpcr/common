// -----------------------------------------------------------------------
// <copyright file="MetricsTestStepsLogger.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Tests.Steps;

using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

internal static partial class MetricsTestStepsLogger
{
    [LoggerMessage(
        0,
        LogLevel.Information,
        "Initialized scenario context, test name = {testName}..." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ScenarioContextInitialized(
        this ILogger logger,
        string testName,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        1,
        LogLevel.Information,
        "Checking listening port {port}..." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void CheckingListeningPort(
        this ILogger logger,
        int port,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        2,
        LogLevel.Information,
        "port {port} is listening!" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void PortListening(
        this ILogger logger,
        int port,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        3,
        LogLevel.Error,
        "port {port} is NOT listening: {error}" +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void PortNotListening(
        this ILogger logger,
        int port,
        string error,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        4,
        LogLevel.Information,
        "Starting API call to {url}..." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void StartingApiCall(
        this ILogger logger,
        DateTime startTime,
        string? url,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        5,
        LogLevel.Information,
        "API call to {url} completed in {elapsed} ms." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void ApiCallCompleted(
        this ILogger logger,
        DateTime startTime,
        string? url,
        double elapsed,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        6,
        LogLevel.Error,
        "API call to {url} failed in {elapsed} ms. error: {error}" +
        ", \n\tcalled from {memberName}, in file {callerFile}")]
    public static partial void ApiCallFailed(
        this ILogger logger,
        DateTime startTime,
        string? url,
        double elapsed,
        string error,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "");

    [LoggerMessage(
        7,
        LogLevel.Information,
        "Starting nested call..." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void StartingNestedCall(
        this ILogger logger,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);

    [LoggerMessage(
        8,
        LogLevel.Information,
        "Nested call completed in {elapsed} ms." +
        ", \n\tcalled from {memberName}, in file {callerFile}, at line {lineNumber}")]
    public static partial void NestedCallCompleted(
        this ILogger logger,
        double elapsed,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0);
}