// -----------------------------------------------------------------------
// <copyright file="HostExtensions.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Hosts;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

public static class HostExtensions
{
    public static Task OnShutDown(this IHost host)
    {
        // allow telemetry to escape before we shutdown
        return Task.Delay(TimeSpan.FromSeconds(10));
    }
}