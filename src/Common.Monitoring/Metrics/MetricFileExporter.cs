// -----------------------------------------------------------------------
// <copyright file="MetricFileExporter.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Metrics;

using System;
using System.Text;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using Sinks;

public class MetricFileExporter : BaseExporter<Metric>
{
    private readonly RollingFileLogger fileLogger;

    public MetricFileExporter(FileSinkSettings fileSink)
    {
        this.fileLogger = new RollingFileLogger(fileSink, "metric");
    }

    public override ExportResult Export(in Batch<Metric> batch)
    {
        if (batch.Count == 0)
        {
            return ExportResult.Success;
        }

        using var scope = SuppressInstrumentationScope.Begin();
        var sb = new StringBuilder();
        foreach (var metric in batch)
        {
            if (sb.Length > 0)
            {
                sb.Append(", ");
            }

            sb.Append($"{metric.Name}: ");

            foreach (ref readonly var metricPoint in metric.GetMetricPoints())
            {
                sb.Append($"{metricPoint.StartTime}\n");
                foreach (var metricPointTag in metricPoint.Tags)
                {
                    sb.Append($"\t{metricPointTag.Key}={metricPointTag.Value}\n");
                }
            }
        }

        Console.WriteLine($"Metrics: \n{sb}");
        this.fileLogger.Log(sb.ToString());
        return ExportResult.Success;
    }
}