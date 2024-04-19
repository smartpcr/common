// -----------------------------------------------------------------------
// <copyright file="IBaseEntity.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Shared;

using System;

public interface IBaseEntity
{
    /// <summary>
    /// Gets system-generated id
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets consistency
    /// </summary>
    string ETag { get; }

    /// <summary>
    /// Gets auto-generated timestamp
    /// </summary>
    DateTime TS { get; }

    /// <summary>
    /// Gets used to track soft deletes
    /// </summary>
    public bool? Removed { get; }

    /// <summary>
    /// Gets azure Cosmos DB to distribute and store documents across different physical partitions.
    /// </summary>
    public string GetPartitionKeyValue();
}