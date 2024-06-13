// -----------------------------------------------------------------------
// <copyright file="KustoMaterializedView.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Kusto;

using System;

public class KustoMaterializedView
{
    public string Name { get; set; }
    public string Folder { get; set; }
    public string SourceTableName { get; set; }
    public string Query { get; set; }
    public bool AutoUpdateSchema { get; set; }
    public TimeSpan? Lookback { get; set; }
}