// -----------------------------------------------------------------------
// <copyright file="HostBuilderExtensions.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Hosts;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

public static class HostBuilderExtensions
{
    public static IHostBuilder AsJob(this IHostBuilder hostBuilder, string[] args)
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var isProduction = string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase);

        hostBuilder
            .UseEnvironment(env)
            .UseConsoleLifetime()
            .ConfigureAppConfiguration(c =>
            {
                c.AddJsonFile("appsettings.json", false, false);
                if (!isProduction)
                {
                    var overrides = env.Split('.', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var envOverride in overrides)
                    {
                        c.AddJsonFile($"appsettings.{envOverride}.json", true, false);
                    }
                }

                c.AddEnvironmentVariables();
                c.AddCommandLine(args);
            });
        SetResourceLimits();

        return hostBuilder;
    }

    private static void SetResourceLimits()
    {
        ThreadPool.SetMinThreads(100, 100);
    }
}

public interface IExecutor
{
    Task ExecuteAsync(CancellationToken cancel);
}