// -----------------------------------------------------------------------
// <copyright file="Connection.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Config.Tests.Models;

using System.ComponentModel.DataAnnotations;

public class Connection
{
    [Required]
    public string Name { get; set; }

    [Required] public string Host { get; set; } = "localhost";

    [Range(1, 65535)] public int Port { get; set; } = 443;
}