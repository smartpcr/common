// -----------------------------------------------------------------------
// <copyright file="KustoColumn.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Kusto;

using System;

public class KustoColumn
{
    public string Name { get; set; }
    public Type Type { get; set; }
    public string CslType { get; set; }
}