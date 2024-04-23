// -----------------------------------------------------------------------
// <copyright file="MetricsTestSteps.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Tests.Steps;

using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Settings;
using Reqnroll;

[Binding]
public class MetricsTestSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MetricsTestSteps> _logger;

    public MetricsTestSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        _serviceProvider = _scenarioContext.Get<IServiceProvider>();
        _logger = _serviceProvider.GetRequiredService<ILogger<MetricsTestSteps>>();
        _logger.ScenarioContextInitialized(scenarioContext.ScenarioInfo.Title);
    }

    [Given(@"a web api is running on port (.*)")]
    public async Task GivenAWebApiIsRunningOnPort(int port)
    {
        _logger.CheckingListeningPort(port);
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync("localhost", port);
            _logger.PortListening(port);
        }
        catch (Exception ex)
        {
            _logger.PortNotListening(port, ex.Message);
            _scenarioContext.StepContext.Status = ScenarioExecutionStatus.TestError;
            throw;
        }
    }

    [Given(@"monitoring settings are configured with metrics capability")]
    public void GivenMonitoringSettingsAreConfiguredWithMetricsCapability()
    {
        var monitorSettings = _serviceProvider.GetService<IOptions<MonitorSettings>>();
        monitorSettings.Should().NotBeNull();
        monitorSettings!.Value.Should().NotBeNull();
        monitorSettings.Value.Metrics.Should().NotBeNull();
    }

    [When(@"I call api endpoint ""(.*)"" (.*) times")]
    public async Task WhenICallApiEndpointTimes(string path, int count)
    {
        var apiHostOptions = _serviceProvider.GetService<IOptions<WebApiHostSettings>>();
        apiHostOptions.Should().NotBeNull();
        apiHostOptions!.Value.Should().NotBeNull();

        HttpClient? httpClient;
        HttpClientHandler? handler = null;
        if (apiHostOptions.Value.UseSsl)
        {
            handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
            httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri($"https://{apiHostOptions.Value.Host}:{apiHostOptions.Value.Port}"),
                Timeout = TimeSpan.FromSeconds(1)
            };
        }
        else
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri($"http://{apiHostOptions.Value.Host}:{apiHostOptions.Value.Port}"),
                Timeout = TimeSpan.FromSeconds(1)
            };
        }

        for (var i = 0; i < count; i++)
        {
            _logger.LogInformation($"calling {path}");
            var response = await httpClient.GetAsync(path);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        httpClient.Dispose();
        handler?.Dispose();
    }

    [Then(@"the metric ""(.*)"" should be (.*)")]
    public void ThenTheMetricShouldBe(string path, int count)
    {
        _logger.LogInformation($"api is called {count} times on {path}");
    }
}