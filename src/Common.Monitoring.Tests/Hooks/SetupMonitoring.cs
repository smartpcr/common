// -----------------------------------------------------------------------
// <copyright file="SetupMonitoring.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Tests.Hooks
{
    using System;
    using Config;
    using Config.Tests.Hooks;
    using Config.Tests.Mocks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using TechTalk.SpecFlow;
    using TechTalk.SpecFlow.Infrastructure;

    [Binding]
    public class SetupMonitoring
    {
        private readonly ScenarioContext context;
        private readonly ISpecFlowOutputHelper outputHelper;

        public SetupMonitoring(ScenarioContext context, ISpecFlowOutputHelper outputHelper)
        {
            this.context = context;
            this.outputHelper = outputHelper;
        }

        [BeforeScenario(Order = 2)]
        public void SetupMonitor()
        {
            var envName = this.context.Get<string>("envName");
            var services = this.context.GetServices();
            services.AddSingleton<ILoggerFactory, MockedLoggerFactory>();
            var configuration = services.AddConfiguration();
            this.context.Set(configuration);

            services.ConfigureSettings<MonitorSettings>()
                .AddMonitoring(configuration);

            var serviceProvider = services.BuildServiceProvider();
            this.context.Set<IServiceProvider>(serviceProvider);
            var logger = serviceProvider.GetRequiredService<ILogger<EnvironmentHook>>();
            logger.StartingInitializer(envName);
            this.outputHelper.WriteInfo($"Monitoring setup for env: {envName}");
        }
    }
}