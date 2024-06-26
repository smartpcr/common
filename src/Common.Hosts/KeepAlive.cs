// -----------------------------------------------------------------------
// <copyright file="KeepAlive.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Hosts;

using System;
using System.Threading;
using System.Threading.Tasks;
using Config;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// generate a metric count every 15 seconds
/// </summary>
public sealed class KeepAlive : BackgroundService
{
    private readonly ILogger<KeepAlive> log;
    private readonly HeartbeatMeter meter;

    public KeepAlive(ILogger<KeepAlive> log, IConfiguration configuration)
    {
        this.log = log;
        var metadata = configuration.GetConfiguredSettings<ApplicationMetadata>();
        this.meter = HeartbeatMeter.Instance(metadata);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            log.Heartbeat();
            this.meter.IncrementHeartbeat();

            var sleepSeconds = 15;

            while (sleepSeconds-- > 0 && !stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        log.HeatbeatStopped();
    }
}