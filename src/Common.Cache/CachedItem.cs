// -----------------------------------------------------------------------
// <copyright file="CachedItem.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache;

using System;

public class CachedItem<T> where T : class, new()
{
    public CachedItem()
    {
        CreatedOn = DateTimeOffset.UtcNow;
    }

    public CachedItem(T value) : this()
    {
        Value = value;
    }

    public DateTimeOffset CreatedOn { get; set; }
    public T Value { get; set; }
}