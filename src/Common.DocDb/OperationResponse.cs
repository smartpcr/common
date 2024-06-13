// -----------------------------------------------------------------------
// <copyright file="OperationResponse.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.DocDb;

using System;

public class OperationResponse<T>
{
    public T Item { get; set; }
    public double RequestUnitsConsumed { get; set; }
    public bool IsSuccessful { get; set; }
    public Exception CosmosException { get; set; }
}