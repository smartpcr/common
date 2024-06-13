// -----------------------------------------------------------------------
// <copyright file="HeartbeatMeter.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Hosts;

using Microsoft.R9.Extensions.Metering;

internal static partial class HeartbeatMeter
{
    [Counter]
    public static partial Heartbeat CreateHeartbeat(IMeter meter);
}