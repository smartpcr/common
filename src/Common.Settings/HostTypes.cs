// -----------------------------------------------------------------------
// <copyright file="HostTypes.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Settings;

public enum HostTypes
{
    /// <summary>
    /// runs indefinitely until ctrl-c is pressed, do not attempt to restart if crashes
    /// </summary>
    Console,

    /// <summary>
    /// runs as web host and exposes and endpoint for requests, attempts to restart if crashes or not responding (using health check),
    /// unless the process is explicitly killed via ctrl-c
    /// </summary>
    WebApi,
}