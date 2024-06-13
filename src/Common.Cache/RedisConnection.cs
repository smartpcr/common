// -----------------------------------------------------------------------
// <copyright file="RedisConnection.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache;

using System;
using Microsoft.Extensions.Logging;
using Shared;
using StackExchange.Redis;

public class RedisConnection : IRedisConnection
{
    private readonly ILogger<RedisConnection> logger;
    private readonly ConfigurationOptions options;
    public string Endpoint { get; }
    public IConnectionMultiplexer Connection { get; set; }

    public RedisConnection(string endpoint, ConfigurationOptions options, IConnectionMultiplexer connectionMultiplexer, ILoggerFactory loggerFactory)
    {
        Endpoint = endpoint;
        this.options = options;
        Connection = connectionMultiplexer;
        logger = loggerFactory.CreateLogger<RedisConnection>();
    }

    public bool EnsureConnected()
    {
        if (Connection.IsConnected)
        {
            return true;
        }

        RetryBlock.Retry(3, TimeSpan.FromSeconds(1), () =>
        {
            Connection.Dispose();
            Connection = ConnectionMultiplexer.Connect(options);
        }, logger, _ => true);

        return Connection.IsConnected;
    }
}