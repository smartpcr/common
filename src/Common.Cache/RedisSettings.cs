// -----------------------------------------------------------------------
// <copyright file="RedisSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache;

public class RedisSettings
{
    public string HostName { get; set; }
    public string AccessKeySecretName { get; set; }
    public string ProtectionCertSecretName { get; set; }

    public string Endpoint => $"{HostName}.redis.cache.windows.net:6380";
}