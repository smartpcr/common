// -----------------------------------------------------------------------
// <copyright file="StorageAuthMode.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Storage;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

[JsonConverter(typeof(StringEnumConverter))]
public enum StorageAuthMode
{
    Msi,
    Spn,
    AuthKeySecretFromVault,
    ConnectionStringFromVault,
    AuthKeyFromEnvironment,
    ConnectionStringFromEnvironment
}