// -----------------------------------------------------------------------
// <copyright file="VaultAuthType.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Settings;

public enum VaultAuthType
{
    /// <summary>
    /// Used in production, Current app is assigned a managed identity, which is grant access to keyvault
    /// </summary>
    Msi,

    /// <summary>
    /// Used in dev environment, make sure user is authenticated via VisualStudio.
    /// or debug in IDE and set env variable DOTNET_RUNNING_IN_CONTAINER, which triggers device code flow
    /// </summary>
    User,

    /// <summary>
    /// Used in dev environment, current app is registered in aad and have access to keyvault,
    /// its access token is retrieved via client secret.
    /// Note: SPN access to keyvault is blocked by conditional access policy in Microsoft tenant. Use with caution.
    /// </summary>
    SpnWithSecretOnFile,

    /// <summary>
    /// Used in dev environment, current app is registered in aad and have access to keyvault,
    /// its access token is retrieved via client cert.
    /// Note: SPN access to keyvault is blocked by conditional access policy in Microsoft tenant. Use with caution.
    /// </summary>
    SpnWithCertOnFile
}