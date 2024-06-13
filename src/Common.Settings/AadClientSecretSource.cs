// -----------------------------------------------------------------------
// <copyright file="AadClientSecretSource.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Settings;

public enum AadClientSecretSource
{
    /// <summary>
    /// Either using managed identity or interactive user login
    /// </summary>
    None,

    /// <summary>
    /// Using SPN with client secret that's stored in local file, usually used for local debug
    /// </summary>
    ClientSecretFromFile,

    /// <summary>
    /// Using SPN with certificate that's stored in local file, usually used for local debug
    /// </summary>
    ClientCertFromFile,

    /// <summary>
    /// Using SPN with client secret stored in key vault, key vault authentication is managed separately
    /// </summary>
    ClientSecretFromVault,

    /// <summary>
    /// Using SPN with certificate that is stored in key vault, key vault authentication is managed separately
    /// </summary>
    ClientCertFromVault
}