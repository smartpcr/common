// -----------------------------------------------------------------------
// <copyright file="ApiRequestMetric.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Tests.Steps;

using System.Diagnostics.Metrics;
using Microsoft.Extensions.AmbientMetadata;

public class ApiRequestMetric
{
    private readonly Counter<long> _totalRequests;
    private readonly Counter<long> _totalSuccesses;
    private readonly Counter<long> _totalFailures;
    private readonly Histogram<double> _requestLatency;

    private ApiRequestMetric(ApplicationMetadata metadata)
    {
        Meter meter = new Meter($"{metadata.ApplicationName}.ApiRequestMetric");
        _totalRequests = meter.CreateCounter<long>("TotalRequests", "Total number of requests");
        _totalSuccesses = meter.CreateCounter<long>("SuccessfulRequests", "Total number of successful requests");
        _totalFailures = meter.CreateCounter<long>("FailedRequests", "Total number of failed requests");
        _requestLatency = meter.CreateHistogram<double>("RequestLatency", "Request latency in milliseconds");
    }

    public static ApiRequestMetric Instance(ApplicationMetadata metadata)
    {
        return new ApiRequestMetric(metadata);
    }

    public void IncrementTotalRequests()
    {
        _totalRequests.Add(1);
    }

    public void IncrementSuccessfulRequests()
    {
        _totalSuccesses.Add(1);
    }

    public void IncrementFailedRequests()
    {
        _totalFailures.Add(1);
    }

    public void RecordRequestLatency(double callLatencyInMs)
    {
        _requestLatency.Record(callLatencyInMs);
    }
}