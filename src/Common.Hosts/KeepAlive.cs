// -----------------------------------------------------------------------
// <copyright file="KeepAlive.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Hosts;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.R9.Extensions.Metering;

/// <summary>
/// generate a metric count every 15 seconds
/// </summary>
public sealed class KeepAlive : BackgroundService
{
    private readonly ILogger<KeepAlive> log;
    private readonly Heartbeat heartbeat;

    public KeepAlive(ILogger<KeepAlive> log, IMeter meter)
    {
        this.log = log;
        this.heartbeat = HeartbeatMeter.CreateHeartbeat(meter);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            log.Heartbeat();
            heartbeat.Add(1);

            var sleepSeconds = 15;

            while (sleepSeconds-- > 0 && !stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        log.HeatbeatStopped();
    }
}