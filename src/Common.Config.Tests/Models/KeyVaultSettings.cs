// -----------------------------------------------------------------------
// <copyright file="KeyVaultSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Config.Tests.Models;

using System.ComponentModel.DataAnnotations;

public class KeyVaultSettings
{
    [Required]
    public string VaultName { get; set; }
    public AuthMode AuthMode { get; set; } = AuthMode.Spn;
}