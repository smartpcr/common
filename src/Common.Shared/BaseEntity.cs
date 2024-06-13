// -----------------------------------------------------------------------
// <copyright file="BaseEntity.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Shared;

using System;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

public abstract class BaseEntity : IBaseEntity
{
    [JsonProperty("id", Order = -10)]
    public string Id { get; set; } = Guid.NewGuid().ToString("D");

    [JsonProperty(PropertyName = "_ts")]
    [JsonConverter(typeof(UnixEpochTimeConverter))]
    public DateTime TS { get; set; } = DateTime.UtcNow; // this will always be overwritten by docdb

    [JsonProperty(PropertyName = "_etag")]
    [NotMapped]
    public string ETag { get; set; }

    [JsonProperty(PropertyName = "_self")]
    [NotMapped]
    public string SelfLink { get; set; }

    [NotMapped]
    public bool? Removed { get; set; }

    public abstract string GetPartitionKeyValue();
}