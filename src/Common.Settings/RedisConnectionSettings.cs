// -----------------------------------------------------------------------
// <copyright file="RedisConnectionSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Settings;

public class RedisConnectionSettings
{
    public string HostName { get; set; }
    public string ConnectionStringSecretName { get; set; }
}