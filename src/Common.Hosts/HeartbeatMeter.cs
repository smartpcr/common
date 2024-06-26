// -----------------------------------------------------------------------
// <copyright file="HeartbeatMeter.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Hosts;

using System.Diagnostics.Metrics;
using Microsoft.Extensions.AmbientMetadata;

internal class HeartbeatMeter
{
    private readonly Counter<long> _heartbeat;

    public HeartbeatMeter(ApplicationMetadata metadata)
    {
        var meter = new Meter($"{metadata.ApplicationName}.{nameof(HeartbeatMeter)}", metadata.BuildVersion);
        this._heartbeat = meter.CreateCounter<long>("heartbeat", "Heartbeat");
    }

    public static HeartbeatMeter Instance(ApplicationMetadata metadata)
    {
        return new HeartbeatMeter(metadata);
    }

    public void IncrementHeartbeat()
    {
        this._heartbeat.Add(1);
    }
}