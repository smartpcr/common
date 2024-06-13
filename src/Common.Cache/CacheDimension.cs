// -----------------------------------------------------------------------
// <copyright file="CacheDimension.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache;

internal class CacheDimension
{
    public string Key { get; set; }
    public string CacheType { get; set; }

    public CacheDimension(string type, string key)
    {
        CacheType = type;
        Key = key;
    }
}