// -----------------------------------------------------------------------
// <copyright file="MetricFileParser.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Metrics
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using OpenTelemetry.Metrics;

    public class SimpleMetric
    {
        public string Name { get; set; }
        public MetricType MetricType { get; set; }
        public DateTimeOffset? TimeStamp { get; set; }
        public Dictionary<string, string> Tags { get; set; }
        public long? LongValue { get; set; }
        public double? DoubleValue { get; set; }
        public int? HistogramCount { get; set; }
        public double? HistogramSum { get; set; }

        public SimpleMetric(string name, MetricType metricType)
        {
            this.Name = name;
            this.MetricType = metricType;
            this.Tags = new Dictionary<string, string>();
        }
    }

    public class MetricFileParser
    {
        private readonly string metricFilePath;

        public MetricFileParser(string metricFilePath)
        {
            this.metricFilePath = metricFilePath;
        }

        public List<SimpleMetric> Parse()
        {
            var lines = File.ReadAllLines(this.metricFilePath);
            var metrics = new List<SimpleMetric>();
            SimpleMetric? currentMetric = null;
            var metricNameTypeRegex = new Regex(@"^([a-z\.0-9_\-]+)\[(\w+)\]\:$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var valueRegex = new Regex(@"(Value|Histogram value|Histogram count): ([0-9\.]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                // Check if it's a metric name line
                if (metricNameTypeRegex.IsMatch(line))
                {
                    if (currentMetric != null)
                    {
                        metrics.Add(currentMetric);
                    }
                    var metricName = metricNameTypeRegex.Match(line).Groups[1].Value;
                    var metricTypeName = metricNameTypeRegex.Match(line).Groups[2].Value;
                    if (Enum.TryParse(typeof(MetricType), metricTypeName, true, out var metricTypeEnum) &&
                        metricTypeEnum is MetricType metricType)
                    {
                        currentMetric = new SimpleMetric(metricName, metricType);
                    }
                }
                else if (line.StartsWith("\tStart Time:"))
                {
                    if (currentMetric != null)
                    {
                        var timestampString = line.Substring("\tStart Time:".Length).Trim();
                        if (DateTimeOffset.TryParse(timestampString, out var timeStamp))
                        {
                            currentMetric.TimeStamp = timeStamp;
                        }
                    }
                }
                else if (line.StartsWith("\t\tTag:"))
                {
                    if (currentMetric != null)
                    {
                        var tagParts = line.Split(new[] { " = " }, StringSplitOptions.RemoveEmptyEntries);
                        if (tagParts.Length == 2 && !currentMetric.Tags.ContainsKey(tagParts[0].Trim()))
                        {
                            currentMetric.Tags.Add(tagParts[0].Trim(), tagParts[1].Trim());
                        }
                    }
                }
                else if (valueRegex.IsMatch(line))
                {
                    if (currentMetric != null)
                    {
                        var doubleValue = double.Parse(valueRegex.Match(line).Groups[2].Value);
                        switch (currentMetric.MetricType)
                        {
                            case MetricType.LongSum:
                            case MetricType.LongGauge:
                            case MetricType.LongSumNonMonotonic:
                                currentMetric.LongValue = (long)doubleValue;
                                break;
                            case MetricType.DoubleGauge:
                            case MetricType.DoubleSum:
                            case MetricType.DoubleSumNonMonotonic:
                                currentMetric.DoubleValue = doubleValue;
                                break;
                            case MetricType.Histogram:
                                var valueType = valueRegex.Match(line).Groups[1].Value;
                                if (valueType == "Histogram count")
                                {
                                    currentMetric.HistogramCount = (int)doubleValue;
                                }
                                else
                                {
                                    currentMetric.HistogramSum = doubleValue;
                                }
                                break;
                        }
                    }


                }
            }

            if (currentMetric != null)
            {
                metrics.Add(currentMetric);
            }

            return metrics;
        }
    }
}