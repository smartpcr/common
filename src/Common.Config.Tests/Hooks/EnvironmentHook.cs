// -----------------------------------------------------------------------
// <copyright file="EnvironmentHook.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Config.Tests.Hooks;

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocks;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Infrastructure;

/// <summary>
/// Make sure IConfiguration and ILoggerFactory are registered in ScenarioContext
/// </summary>
[Binding]
public class EnvironmentHook
{
    private readonly ScenarioContext context;
    private readonly ISpecFlowOutputHelper outputHelper;

    public EnvironmentHook(ScenarioContext scenarioContext, ISpecFlowOutputHelper outputHelper)
    {
        this.context = scenarioContext;
        this.outputHelper = outputHelper;
    }

    [BeforeScenario("dev")]
    public void SetupDevEnv()
    {
        SetupEnv("Development");
    }

    [BeforeScenario("prod")]
    public void SetupProdEnv()
    {
        SetupEnv("Production");
    }

    private void SetupEnv(string envName)
    {
        outputHelper.WriteLine($"Use {envName} environment");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", envName, EnvironmentVariableTarget.Process);
        ConfigureServices();
    }

    private void ConfigureServices()
    {
        var services = this.context.GetServices();
        services.AddSingleton<ILoggerFactory, MockedLoggerFactory>();
        var configuration = services.AddConfiguration();
        this.context.Set(configuration);
    }
}