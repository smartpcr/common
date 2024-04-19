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
    public static IServiceCollection AddR9Monitoring(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddR9Logging(configuration)
            .AddR9Metrics(configuration)
            .AddR9Tracing(configuration);
        return services;
    }

    public static void UseR9Monitoring(this IApplicationBuilder app)
    {
        app.UseR9Metrics();
    }
}