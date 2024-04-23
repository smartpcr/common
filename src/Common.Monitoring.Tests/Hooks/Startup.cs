// -----------------------------------------------------------------------
// <copyright file="Startup.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Tests.Hooks;

using System;
using Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Reqnroll;

internal class Startup
{
    private readonly ScenarioContext _scenarioContext;
    private readonly string _envName;

    public Startup(ScenarioContext scenarioContext, string envName)
    {
        _scenarioContext = scenarioContext;
        _envName = envName;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        var configuration = services.AddConfiguration();
        services.ConfigureSettings<MonitorSettings>().AddR9Monitoring(configuration);

        var serviceProvider = services.BuildServiceProvider();
        _scenarioContext.Set<IServiceProvider>(serviceProvider);
        var logger = serviceProvider.GetRequiredService<ILogger<Startup>>();
        logger.StartingInitializer(_envName);

        services.AddControllers();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseR9Monitoring();
    }
}