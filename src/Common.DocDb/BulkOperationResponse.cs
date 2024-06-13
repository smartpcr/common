// -----------------------------------------------------------------------
// <copyright file="BulkOperationResponse.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.DocDb;

using System;
using System.Collections.Generic;

public class BulkOperationResponse<T>
{
    public TimeSpan TotalTimeTaken { get; set; }
    public int SuccessfulDocuments { get; set; }
    public double TotalRequestUnitsConsumed { get; set; }

    public IReadOnlyList<(T item, Exception exception)> Failures { get; set; }
}