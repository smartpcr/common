// -----------------------------------------------------------------------
// <copyright file="MonitorBuilder.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring;

using Common.Config;
using Logs;
using Metrics;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tracing;

public static class MonitorBuilder
{
    public static IServiceCollection AddMonitoring(this IServiceCollection services, IConfiguration configuration)
    {
        var metadata = configuration.GetConfiguredSettings<ApplicationMetadata>();
        var diagnosticsConfig = new DiagnosticsConfig(metadata);
        services.AddSingleton(diagnosticsConfig);

        return services.AddLogging(configuration)
            .AddMetrics(configuration)
            .AddTracing(configuration);
    }
}