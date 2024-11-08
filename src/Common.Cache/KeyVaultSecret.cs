// -----------------------------------------------------------------------
// <copyright file="KeyVaultSecret.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache
{
    public class KeyVaultSecret
    {
        public string SecretName { get; set; }
        public string Value { get; set; }
    }
}
