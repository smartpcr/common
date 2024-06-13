// -----------------------------------------------------------------------
// <copyright file="VaultSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Settings;

using System;
using System.ComponentModel.DataAnnotations;

public class VaultSettings
{
    [Required]
    public string VaultName { get; set; }
    public Uri VaultUrl => new Uri($"https://{VaultName}.vault.azure.net");
    public VaultAuthType AuthType { get; set; } = VaultAuthType.Msi;

    /// <summary>
    /// Gets or sets AAD settings for accessing the vault.
    /// When this is not set, it uses default AAD settings.
    /// </summary>
    public VaultAadSettings? Aad { get; set; }
}