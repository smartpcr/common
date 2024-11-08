// -----------------------------------------------------------------------
// <copyright file="RedisCacheSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache;

public class RedisCacheSettings
{
    public KeyVaultSecret AccessKeySecret { get; set; }
    public KeyVaultSecret ProtectionCertSecret { get; set; }
    public string Endpoint { get; set; }
}