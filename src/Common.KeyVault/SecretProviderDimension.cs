// -----------------------------------------------------------------------
// <copyright file="SecretProviderDimension.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.KeyVault;


internal class SecretProviderDimension
{
    public VaultAuthType AuthType { get; set; }

    public SecretProviderDimension(VaultAuthType authType)
    {
        AuthType = authType;
    }
}