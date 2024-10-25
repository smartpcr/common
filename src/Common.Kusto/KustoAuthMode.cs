// -----------------------------------------------------------------------
// <copyright file="KustoAuthMode.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Kusto;

public enum KustoAuthMode
{
    Msi,
    Spn,
    User,
    None
}