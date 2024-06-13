// -----------------------------------------------------------------------
// <copyright file="IRedisConnectionFactory.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache;

using System.Threading;
using System.Threading.Tasks;

public interface IRedisConnectionFactory
{
    Task<IRedisConnection> GetConnectionAsync(string key, CancellationToken cancel);
}