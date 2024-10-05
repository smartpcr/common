// -----------------------------------------------------------------------
// <copyright file="BulkInsertMode.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Kusto
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    [JsonConverter(typeof(StringEnumConverter))]
    public enum BulkInsertMode
    {
        Direct,
        Stream,
        Queued
    }
}