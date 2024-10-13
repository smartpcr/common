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

    [BeforeScenario("dev", Order = 1)]
    public void SetupDevEnv()
    {
        this.SetupEnv("Development");
    }

    [BeforeScenario("prod", Order = 1)]
    public void SetupProdEnv()
    {
        this.SetupEnv("Production");
    }

    [AfterScenario(Order = int.MaxValue)]
    public void AfterScenario(ScenarioContext scenarioContext)
    {
        foreach (var item in scenarioContext)
        {
            // other disposable items
            if (item.Value is IDisposable disposableItem)
            {
                this.outputHelper.WriteLine($"Disposing {item.Key}...");
                disposableItem.Dispose();
            }
        }
    }

    private void SetupEnv(string envName)
    {
        this.outputHelper.WriteLine($"Use {envName} environment");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", envName, EnvironmentVariableTarget.Process);
        this.ConfigureServices(envName);
    }

    private void ConfigureServices(string envName)
    {
        var services = this.context.GetServices();
        var configuration = services.AddConfiguration();
        this.context.Set(configuration);
        this.context.Set(envName, "envName");
    }
}