// -----------------------------------------------------------------------
// <copyright file="BaseEventSource.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.ETW;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Trace;

/// <summary>
/// Base event source class.
/// </summary>
public abstract class BaseEventSource : EventSource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseEventSource"/> class.
    /// </summary>
    /// <param name="eventSourceName">ETW provider name</param>
    protected BaseEventSource(string eventSourceName) : base(eventSourceName)
    {
        this.TraceProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(eventSourceName)
            .Build();
    }

    /// <summary>
    /// Gets the tracer provider.
    /// </summary>
    protected TracerProvider TraceProvider { get; }

    /// <summary>
    /// Write log, event and trace, associate event with current span.
    /// </summary>
    /// <param name="eventId">Event id</param>
    /// <param name="logger">ILogger instance</param>
    /// <param name="level">Log level</param>
    /// <param name="messageTemplate">Log message template</param>
    /// <param name="args">Values passed to format message.</param>
    /// <param name="callerFile">Call from file name</param>
    /// <param name="lineNumber">Line number of caller</param>
    /// <param name="tags">Event tags</param>
    /// <param name="logMethodName">Log method name</param>
    public TelemetrySpan UsingTraceEvent(
        int eventId,
        ILogger logger,
        LogLevel level,
        string messageTemplate = "",
        object[]? args = null,
        List<KeyValuePair<string, string>>? tags = null,
        [CallerFilePath] string callerFile = "",
        [CallerMemberName] string logMethodName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        using (logger.BeginScope(new Dictionary<string, object>
               {
                   ["SpanId"] = Activity.Current?.SpanId.ToString() ?? string.Empty,
                   ["TraceId"] = Activity.Current?.TraceId.ToString() ?? string.Empty,
                   ["ParentId"] = Activity.Current?.ParentSpanId.ToString() ?? string.Empty,
                   ["CallerFile"] = callerFile,
                   ["MemberName"] = logMethodName,
                   ["LineNumber"] = lineNumber
               }))
        {
            logger.Log(level, messageTemplate, args ?? Array.Empty<object>());
        }

        var tracer = this.TraceProvider.GetTracer(this.Name);
        var span = tracer.StartActiveSpan(logMethodName);
        if (tags == null)
        {
            tags = new List<KeyValuePair<string, string>>();
        }

        foreach (var tag in tags)
        {
            span.SetAttribute(tag.Key, tag.Value);
        }

        tags.Add(new KeyValuePair<string, string>("EventName", logMethodName));
        this.WriteEvent(eventId, tags);

        span.AddEvent(logMethodName);

        return span;
    }
}