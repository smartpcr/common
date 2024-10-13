// -----------------------------------------------------------------------
// <copyright file="MetricsTestSteps.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Tests.Steps;

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TechTalk.SpecFlow;

[Binding]
public class MetricsTestSteps
{
    private readonly ScenarioContext scenarioContext;
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<MetricsTestSteps> logger;

    public MetricsTestSteps(ScenarioContext scenarioContext)
    {
        this.scenarioContext = scenarioContext;
        this.serviceProvider = this.scenarioContext.Get<IServiceProvider>();
        this.logger = this.serviceProvider.GetRequiredService<ILogger<MetricsTestSteps>>();
        this.logger.ScenarioContextInitialized(scenarioContext.ScenarioInfo.Title);
    }

    [Given(@"monitoring settings are configured with metrics capability")]
    public void GivenMonitoringSettingsAreConfiguredWithMetricsCapability()
    {
        var monitorSettings = this.serviceProvider.GetService<IOptions<MonitorSettings>>();
        monitorSettings.Should().NotBeNull();
        monitorSettings!.Value.Should().NotBeNull();
        monitorSettings.Value.Metrics.Should().NotBeNull();
    }

    [Given(@"setup api handler for request ""([^""]+)"" to return ""([^""]+)""")]
    public void GivenSetupApiHandlerForRequestToReturn(string path, string responseMessage)
    {
        Func<HttpListenerContext, Task> handlerFunc = async context =>
        {
            await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes($"{responseMessage} {DateTime.UtcNow}"));
        };
        this.scenarioContext.Add($"{HttpMethod.Get} {path}", handlerFunc);
    }

    [When(@"I call api endpoint ""(.*)"" (.*) times")]
    public async Task WhenICallApiEndpointTimes(string path, int count)
    {
        var httpClient = this.scenarioContext.Get<HttpClient>();

        for (var i = 0; i < count; i++)
        {
            this.logger.LogInformation($"calling {path}");
            var response = await httpClient.GetAsync(path);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        httpClient.Dispose();
    }

    [Then(@"the metric ""(.*)"" should be (.*)")]
    public void ThenTheMetricShouldBe(string path, int count)
    {
        this.logger.LogInformation($"api is called {count} times on {path}");
    }
}