﻿// -----------------------------------------------------------------------
// <copyright file="FeatureHook.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Kusto.Tests.Hooks
{
    using Config;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Monitoring;
    using TechTalk.SpecFlow;

    [Binding]
    public sealed class FeatureHook
    {
        [BeforeFeature]
        public static void SetupKustoConnection(FeatureContext featureContext)
        {
            var services = new ServiceCollection();
            var configuration = services.AddConfiguration();
            services.AddSingleton<ILoggerFactory, MockedLoggerFactory>();
            services.AddMonitoring(configuration);
            var serviceProvider = services.BuildServiceProvider();
            var kustoSettings = configuration.GetConfiguredSettings<KustoSettings>();
            featureContext.Set(kustoSettings);

            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            IKustoClient kustoClient = new KustoClient(serviceProvider, loggerFactory, kustoSettings);
            featureContext.Set(kustoClient);
        }

        [AfterFeature]
        public static void CleanupKustoConnection(FeatureContext featureContext)
        {
            if (featureContext.TryGetValue(out IKustoClient kustoClient))
            {
                kustoClient.Dispose();
            }
        }
    }
}