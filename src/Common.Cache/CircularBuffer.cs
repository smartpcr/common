// -----------------------------------------------------------------------
// <copyright file="CircularBuffer.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class CircularBuffer<T> : BlockingCollection<T>, IBoundedQueue<T>
{
    private long totalDropped;

    public long TotalDropped => totalDropped;
    public int Total => Count;

    public CircularBuffer(int boundedCapacity) : base(boundedCapacity)
    {
        totalDropped = 0;
    }

    public T? Read() => TryTake(out var value) ? value : default;

    public Task<IEnumerable<T?>> TakeAsync(int size, CancellationToken cancel = default)
    {
        var output = new List<T?>();
        var item = Read();

        while (!cancel.IsCancellationRequested && item != null)
        {
            output.Add(item);
            if (output.Count >= size)
            {
                break;
            }
            item = Read();
        }

        return Task.FromResult(output.AsEnumerable());
    }

    public Task<(bool added, int dropped)> WriteAsync(T value, CancellationToken cancel = default)
    {
        var dropped = 0;
        while (!cancel.IsCancellationRequested && !TryAdd(value))
        {
            Read();
            dropped++;
            Interlocked.Increment(ref totalDropped);
        }
        return Task.FromResult((true, dropped));
    }
}