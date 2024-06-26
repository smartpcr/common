// -----------------------------------------------------------------------
// <copyright file="TestController.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Tests.Steps;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Config;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("api")]
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> _logger;
    private readonly ApiRequestMetric _apiRequestMetric;

    public TestController(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        this._logger = loggerFactory.CreateLogger<TestController>();
        var metadata = configuration.GetConfiguredSettings<ApplicationMetadata>();
        this._apiRequestMetric = ApiRequestMetric.Instance(metadata);
    }

    [HttpGet("hello")]
    public async Task<string> SayHello()
    {
        _logger.StartingApiCall(DateTime.Now, Request.Path.Value);
        this._apiRequestMetric.IncrementTotalRequests();

        var watch = Stopwatch.StartNew();
        bool hasError = DateTime.Now.Second % 5 == 0;
        await InnerCall();
        if (hasError)
        {
            this._apiRequestMetric.IncrementFailedRequests();
            _logger.ApiCallFailed(DateTime.Now, Request.Path.Value, watch.ElapsedMilliseconds, "Simulated error");
            throw new InvalidOperationException("Simulated error");
        }

        this._apiRequestMetric.IncrementSuccessfulRequests();
        await Task.Delay(100);
        this._apiRequestMetric.RecordRequestLatency(watch.ElapsedMilliseconds);
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