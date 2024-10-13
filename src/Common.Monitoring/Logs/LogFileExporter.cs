// -----------------------------------------------------------------------
// <copyright file="FileExporter.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Logs
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.Logging;
    using OpenTelemetry;
    using OpenTelemetry.Logs;
    using Sinks;

    public class LogFileExporter : BaseExporter<LogRecord>
    {
        private readonly LogLevel logLevel;
        private readonly RollingFileLogger fileLogger;

        public LogFileExporter(FileSinkSettings fileSink, LogLevel logLevel)
        {
            this.logLevel = logLevel;
            this.fileLogger = new RollingFileLogger(fileSink, "log");
        }

        public override ExportResult Export(in Batch<LogRecord> batch)
        {
            var fileLogEntries = new List<string>();
            foreach (var record in batch)
            {
                if (record.LogLevel < this.logLevel)
                {
                    continue;
                }

                var logMessage = $"{record.Timestamp:o}: {record.CategoryName} [{record.LogLevel}] {record.FormattedMessage}\n";
                fileLogEntries.Add(logMessage);
            }

            if (fileLogEntries.Any())
            {
                this.fileLogger.Log(fileLogEntries);
            }

            return ExportResult.Success;
        }
    }
}