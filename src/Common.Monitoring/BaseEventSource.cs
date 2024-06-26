// -----------------------------------------------------------------------
// <copyright file="BaseEventSource.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring;

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
    /// Write event and trace.
    /// </summary>
    /// <param name="eventId">Event id</param>
    /// <param name="logger">ILogger instance</param>
    /// <param name="level">Log level</param>
    /// <param name="callerFile">Call from file name</param>
    /// <param name="memberName">Call from method name</param>
    /// <param name="lineNumber">Line number of caller</param>
    /// <param name="messageTemplate">Log message template</param>
    /// <param name="tags">Event tags</param>
    /// <param name="logMethodName">Log method name</param>
    public TelemetrySpan UsingTraceEvent(
        int eventId,
        ILogger logger,
        LogLevel level,
        string callerFile,
        string memberName,
        int lineNumber,
        string messageTemplate = "",
        List<KeyValuePair<string, string>>? tags = null,
        [CallerMemberName] string logMethodName = "")
    {
        using (logger.BeginScope(new Dictionary<string, object>
               {
                   ["SpanId"] = Activity.Current?.SpanId.ToString() ?? string.Empty,
                   ["TraceId"] = Activity.Current?.TraceId.ToString() ?? string.Empty,
                   ["ParentId"] = Activity.Current?.ParentSpanId.ToString() ?? string.Empty,
                   ["CallerFile"] = callerFile,
                   ["MemberName"] = memberName,
                   ["LineNumber"] = lineNumber
               }))
        {
            logger.Log(level, messageTemplate, tags);
        }

        Tracer tracer = this.TraceProvider.GetTracer(this.Name);
        TelemetrySpan scope = tracer.StartActiveSpan(memberName);
        if (tags == null)
        {
            tags = new List<KeyValuePair<string, string>>();
        }

        foreach (KeyValuePair<string, string> tag in tags)
        {
            scope.SetAttribute(tag.Key, tag.Value);
        }

        tags.Add(new KeyValuePair<string, string>("EventName", logMethodName));
        this.WriteEvent(eventId, tags);

        return scope;
    }
}