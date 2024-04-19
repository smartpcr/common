// -----------------------------------------------------------------------
// <copyright file="ProxySettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Settings;

using System;

public class ProxySettings
{
    public Uri ProxyUri { get; set; }
    public bool BypassProxyOnLocal { get; set; } = true;
}