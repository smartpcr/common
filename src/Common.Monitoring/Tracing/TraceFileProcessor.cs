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
    private readonly RollingFileLogger _fileLogger;

    public TraceFileProcessor(FileSinkSettings fileSink)
    {
        _fileLogger = new RollingFileLogger(fileSink);
    }

    public override void OnEnd(Activity data)
    {
        var traceData = $"{DateTime.UtcNow:o} Id: {data.Id}, Trace: \n\t{JsonConvert.SerializeObject(data)}\n";
        _fileLogger.Log(traceData);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _fileLogger.Dispose();
        }
    }
}