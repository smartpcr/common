// -----------------------------------------------------------------------
// <copyright file="TraceFileProcessor.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Tracing;

using System;
using System.Diagnostics;
using Newtonsoft.Json;
using OpenTelemetry;
using Sinks;

public class TraceFileProcessor : BaseProcessor<Activity>
{
    private readonly RollingFileLogger fileLogger;

    public TraceFileProcessor(FileSinkSettings fileSink)
    {
        this.fileLogger = new RollingFileLogger(fileSink, "trace");
    }

    public override void OnEnd(Activity data)
    {
        var traceData = $"{DateTime.UtcNow:o} Id: {data.Id}, Trace: \n\t{JsonConvert.SerializeObject(data)}\n";
        this.fileLogger.Log(traceData);
    }
}