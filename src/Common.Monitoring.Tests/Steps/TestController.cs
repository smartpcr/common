// -----------------------------------------------------------------------
// <copyright file="TestController.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Tests.Steps;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.R9.Extensions.Metering;

[ApiController]
[Route("api")]
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> _logger;
    private readonly TotalRequests _totalRequests;
    private readonly SuccessfulRequests _totalSuccesses;
    private readonly FailedRequests _totalFailures;
    private readonly RequestLatency _requestLatency;

    public TestController(ILoggerFactory loggerFactory, IMeterProvider meterProvider)
    {
        _logger = loggerFactory.CreateLogger<TestController>();
        var meter = meterProvider.GetMeter<TestController>();
        _totalRequests = ApiRequestMetric.CreateTotalRequests(meter);
        _totalSuccesses = ApiRequestMetric.CreateSuccessfulRequests(meter);
        _totalFailures = ApiRequestMetric.CreateFailedRequests(meter);
        _requestLatency = ApiRequestMetric.CreateRequestLatency(meter);
    }

    [HttpGet("hello")]
    public async Task<string> SayHello()
    {
        _logger.StartingApiCall(DateTime.Now, Request.Path.Value);
        _totalRequests.Add(1);

        var watch = Stopwatch.StartNew();
        bool hasError = DateTime.Now.Second % 5 == 0;
        await InnerCall();
        if (hasError)
        {
            _totalFailures.Add(1);
            _logger.ApiCallFailed(DateTime.Now, Request.Path.Value, watch.ElapsedMilliseconds, new InvalidOperationException("Simulated error"));
            throw new InvalidOperationException("Simulated error");
        }

        _totalSuccesses.Add(1);
        await Task.Delay(100);
        _requestLatency.Record(watch.ElapsedMilliseconds);
        _logger.ApiCallCompleted(DateTime.Now, Request.Path.Value, watch.ElapsedMilliseconds);

        return "Hello World";
    }

    private async Task InnerCall()
    {
        _logger.StartingNestedCall();
        var watch = Stopwatch.StartNew();
        await Task.Delay(100);
        _logger.NestedCallCompleted(watch.ElapsedMilliseconds);
    }
}