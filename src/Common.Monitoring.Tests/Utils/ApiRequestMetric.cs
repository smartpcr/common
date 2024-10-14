// -----------------------------------------------------------------------
// <copyright file="ApiRequestMetric.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Tests.Utils;

using System;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;

public class ApiRequestMetric
{
    private static ApiRequestMetric? instance;
    public const string TotalRequests = "total_requests";
    public const string SuccessfulRequests = "successful_requests";
    public const string FailedRequests = "failed_requests";
    public const string RequestLatency = "request_latency";

    private readonly Counter<long> totalRequests;
    private readonly Counter<long> totalSuccesses;
    private readonly Counter<long> totalFailures;
    private readonly Histogram<double> requestLatency;

    private ApiRequestMetric(IServiceProvider serviceProvider)
    {
        var diagnosticsConfig = serviceProvider.GetRequiredService<DiagnosticsConfig>();
        this.totalRequests = diagnosticsConfig.Meter.CreateCounter<long>(TotalRequests, "Total number of requests");
        this.totalSuccesses = diagnosticsConfig.Meter.CreateCounter<long>(SuccessfulRequests, "Total number of successful requests");
        this.totalFailures = diagnosticsConfig.Meter.CreateCounter<long>(FailedRequests, "Total number of failed requests");
        this.requestLatency = diagnosticsConfig.Meter.CreateHistogram<double>(RequestLatency, unit: "ms", description: "Request latency in milliseconds");
    }

    public static ApiRequestMetric Instance(IServiceProvider serviceProvider)
    {
        if (ApiRequestMetric.instance == null)
        {
            ApiRequestMetric.instance = new ApiRequestMetric(serviceProvider);
        }

        return ApiRequestMetric.instance;
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