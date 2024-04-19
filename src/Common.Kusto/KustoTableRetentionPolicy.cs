// -----------------------------------------------------------------------
// <copyright file="KustoTableRetentionPolicy.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Kusto;

using System;

public class KustoTableRetentionPolicy
{
    public bool Enabled { get; set; }
    public TimeSpan SoftDeletePeriod { get; set; }
}