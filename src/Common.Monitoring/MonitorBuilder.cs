// -----------------------------------------------------------------------
// <copyright file="MonitorBuilder.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring;

using Logs;
using Metrics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tracing;

public static class MonitorBuilder
{
    public static IServiceCollection AddMonitoring(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddLogging(configuration)
            .AddMetrics(configuration)
            .AddTracing(configuration);
    }
}