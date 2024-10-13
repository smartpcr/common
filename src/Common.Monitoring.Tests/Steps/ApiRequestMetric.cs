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
    public const string TotalRequests = "total_requests";
    public const string SuccessfulRequests = "successful_requests";
    public const string FailedRequests = "failed_requests";
    public const string RequestLatency = "request_latency";

    private readonly Counter<long> totalRequests;
    private readonly Counter<long> totalSuccesses;
    private readonly Counter<long> totalFailures;
    private readonly Histogram<double> requestLatency;

    private ApiRequestMetric(ApplicationMetadata metadata)
    {
        var meter = new Meter($"{metadata.ApplicationName}.{nameof(ApiRequestMetric)}");
        this.totalRequests = meter.CreateCounter<long>(TotalRequests, "Total number of requests");
        this.totalSuccesses = meter.CreateCounter<long>(SuccessfulRequests, "Total number of successful requests");
        this.totalFailures = meter.CreateCounter<long>(FailedRequests, "Total number of failed requests");
        this.requestLatency = meter.CreateHistogram<double>(RequestLatency, "Request latency in milliseconds");
    }

    public static ApiRequestMetric Instance(ApplicationMetadata metadata)
    {
        return new ApiRequestMetric(metadata);
    }

    public void IncrementTotalRequests()
    {
        this.totalRequests.Add(1);
    }

    public void IncrementSuccessfulRequests()
    {
        this.totalSuccesses.Add(1);
    }

    public void IncrementFailedRequests()
    {
        this.totalFailures.Add(1);
    }

    public void RecordRequestLatency(double callLatencyInMs)
    {
        this.requestLatency.Record(callLatencyInMs);
    }
}