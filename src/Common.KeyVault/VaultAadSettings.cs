// -----------------------------------------------------------------------
// <copyright file="VaultAadSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.KeyVault;

using System.ComponentModel.DataAnnotations;

public class VaultAadSettings
{
    [Required]
    public string TenantId { get; set; }

    /// <summary>
    /// Gets or sets spn application id, only required when AuthType is SpnWithSecret or SpnWithCert.
    /// </summary>
    public string ClientId { get; set; }

    /// <summary>
    /// Gets or sets the secret name for authentication.
    /// This is client secret file name when AuthType is SpnWithSecretOnFile.
    /// This is client cert file name when AuthType is SpnWithCertOnFile.
    /// </summary>
    public string SecretFileName { get; set; }
}