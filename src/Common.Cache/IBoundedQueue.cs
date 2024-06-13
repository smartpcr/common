// -----------------------------------------------------------------------
// <copyright file="IBoundedQueue.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IBoundedQueue<T>
{
    int Count { get; }
    long TotalDropped { get; }
    Task<(bool added, int dropped)> WriteAsync(T value, CancellationToken cancel = default);
    Task<IEnumerable<T?>> TakeAsync(int size, CancellationToken cancel = default);
}