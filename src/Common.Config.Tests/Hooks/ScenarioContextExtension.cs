// -----------------------------------------------------------------------
// <copyright file="ScenarioContextExtension.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Config.Tests.Hooks
{
    using System;
    using Common.Config.Tests.Mocks;
    using FluentAssertions;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Reqnroll;

    internal static class ScenarioContextExtension
    {
        public static IServiceCollection GetServices(this ScenarioContext scenarioContext)
        {
            if (!scenarioContext.TryGetValue(out IServiceCollection services))
            {
                services = EnvironmentHook.GetServiceCollection();
                scenarioContext.Set(services);
            }

            return services;
        }

        public static IConfiguration GetConfiguration(this ScenarioContext scenarioContext)
        {
            if (!scenarioContext.TryGetValue(out IConfiguration configuration))
            {
                var services = scenarioContext.GetServices();
                configuration = services.AddConfiguration();
                scenarioContext.Set(configuration);
            }

            configuration.Should().NotBeNull();
            return configuration!;
        }

        public static ILogger<T> GetLogger<T>(this ScenarioContext scenarioContext)
        {
            if (!scenarioContext.TryGetValue(out ILogger<T>? logger))
            {
                var loggerFactory = scenarioContext.GetLoggerFactory();
                logger = loggerFactory.CreateLogger<T>();
                scenarioContext.Set(logger);
            }

            logger.Should().NotBeNull();
            return logger!;
        }

        public static void WithSingleton<TService, TImplementation>(this ScenarioContext scenarioContext)
            where TService : class
            where TImplementation : class, TService
        {
            var services = scenarioContext.Get<IServiceCollection>();
            services.AddSingleton<TService, TImplementation>();
        }

        internal static void SetupDependencies(this ScenarioContext scenarioContext, Func<IServiceCollection, IServiceCollection> configure)
        {
            var services = scenarioContext.Get<IServiceCollection>();
            configure(services);
        }

        private static MockedLoggerFactory GetLoggerFactory(this ScenarioContext scenarioContext)
        {
            if (!scenarioContext.TryGetValue(out ILoggerFactory loggerFactory))
            {
                var services = scenarioContext.GetServices();
                services.AddSingleton<ILoggerFactory, MockedLoggerFactory>();
                var serviceProvider = services.BuildServiceProvider();
                loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                scenarioContext.Set(loggerFactory);
            }

            loggerFactory.Should().NotBeNull();
            var mockedLoggerFactory = loggerFactory as MockedLoggerFactory;
            mockedLoggerFactory.Should().NotBeNull();
            return mockedLoggerFactory!;
        }
    }
}