// -----------------------------------------------------------------------
// <copyright file="ICacheProvider.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache;

using System;
using System.Threading;
using System.Threading.Tasks;

public interface ICacheProvider
{
    Task<T> GetOrUpdateAsync<T>(
        string key,
        Func<Task<DateTimeOffset>> getLastModificationTime,
        Func<Task<T>> getItem,
        CancellationToken cancel) where T : class, new();

    T GetOrUpdate<T>(
        string key,
        Func<DateTimeOffset> getLastModificationTime,
        Func<T> getItem,
        CancellationToken cancel = default) where T : class, new();

    bool TryGet<T>(string key, out T? item) where T : class, new();

    Task Set<T>(string key, T item, CancellationToken cancel) where T : class, new();

    Task ClearAsync(string key, CancellationToken cancel);

    Task ClearAll(CancellationToken cancel);
}