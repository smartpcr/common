// -----------------------------------------------------------------------
// <copyright file="LogFileProcessor.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Logs;

using OpenTelemetry;
using OpenTelemetry.Logs;
using Sinks;

public class LogFileProcessor : BaseProcessor<LogRecord>
{
    private readonly RollingFileLogger _fileLogger;

    public LogFileProcessor(FileSinkSettings fileSink)
    {
        _fileLogger = new RollingFileLogger(fileSink);
    }

    public override void OnEnd(LogRecord data)
    {
        var logMessage = $"{data.Timestamp:o}: {data.CategoryName} [{data.LogLevel}] {data.FormattedMessage}\n";
        _fileLogger.Log(logMessage);
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