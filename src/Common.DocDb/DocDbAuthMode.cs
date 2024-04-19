// -----------------------------------------------------------------------
// <copyright file="DocDbAuthMode.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.DocDb;

public enum DocDbAuthMode
{
    Msi,
    AuthKeyFromKeyVault,
    AuthKeyFromEnvironment,
    ConnectionStringFromEnvironment
}