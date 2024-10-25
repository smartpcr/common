// -----------------------------------------------------------------------
// <copyright file="ApiClientSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Http;

using System;
using Common.Auth;

public class ApiClientSettings
{
    public string Endpoint { get; set; }
    public HttpClientAuthMode AuthMode { get; set; } = HttpClientAuthMode.Aad;
    public int RetryCount { get; set; } = 3;
    public int CircuitBreakCount { get; set; } = 7;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public AadSettings? Aad { get; set; }
    public ProxySettings? Proxy { get; set; }
}