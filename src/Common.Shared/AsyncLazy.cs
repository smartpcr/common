// -----------------------------------------------------------------------
// <copyright file="AsyncLazy.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Shared;

using System;
using System.Threading;
using System.Threading.Tasks;

public class AsyncLazy<T> : Lazy<Task<T>>
{
    public AsyncLazy(Func<T> valueFactory)
        : base(() => Task.Factory.StartNew(valueFactory, CancellationToken.None, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Default))
    {
    }

    public AsyncLazy(Func<Task<T>> taskFactory) :
        base(() => Task.Factory.StartNew(taskFactory, CancellationToken.None, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Default).Unwrap())
    {
    }
}