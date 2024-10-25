// -----------------------------------------------------------------------
// <copyright file="RedisConnectionFactory.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache;

using System;
using System.Collections.Concurrent;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Config;
using KeyVault;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

public class RedisConnectionFactory : IRedisConnectionFactory
{
    private readonly ILoggerFactory loggerFactory;
    private readonly ILogger<RedisConnectionFactory> logger;
    private readonly IConfiguration configuration;
    private readonly ISecretProvider? secretProvider;

    private readonly ConcurrentDictionary<string, RedisConnection> redisConns =
        new ConcurrentDictionary<string, RedisConnection>();

    public RedisConnectionFactory(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
    {
        this.loggerFactory = loggerFactory;
        logger = loggerFactory.CreateLogger<RedisConnectionFactory>();
        configuration = serviceProvider.GetRequiredService<IConfiguration>();
        secretProvider = serviceProvider.GetService<ISecretProvider>();
    }

    public async Task<IRedisConnection> GetConnectionAsync(string key, CancellationToken cancel)
    {
        if (redisConns.TryGetValue(key, out RedisConnection? existingConn))
        {
            return existingConn;
        }

        var redisConnectionSettings = configuration.GetConfiguredSettings<RedisGroup>();
        var redisConnectionSetting = redisConnectionSettings[key];
        logger.BuildRedisConnection(redisConnectionSetting.ConnectionStringSecretName, key, redisConnectionSetting.HostName);
        if (secretProvider == null)
        {
            throw new InvalidOperationException("SecretProvider is not registered for redis connection");
        }

        var redisConnStrSecret = await secretProvider.GetSecretAsync(redisConnectionSetting.ConnectionStringSecretName, cancel);
        var configurationOptions = ConfigurationOptions.Parse(redisConnStrSecret);
        configurationOptions.ConnectTimeout = 5000; // 5 sec, default is 5 sec
        configurationOptions.SyncTimeout = 10000; // 10 sec, default is 5 sec
        configurationOptions.AsyncTimeout = 15000; // 15 sec, default is 5 sec
        configurationOptions.ConnectRetry = 3;
        configurationOptions.KeepAlive = 180; // 3 min
        configurationOptions.ResolveDns = false;
        configurationOptions.SslProtocols = SslProtocols.Tls12;
        IConnectionMultiplexer connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(configurationOptions);
        var endpoint = $"{redisConnectionSetting.HostName}.redis.cache.windows.net:6380";
        var redisConn = new RedisConnection(endpoint, configurationOptions, connectionMultiplexer, loggerFactory);
        redisConns.AddOrUpdate(key, redisConn, (_, _) => redisConn);
        return redisConn;
    }
}

public interface IRedisConnection
{
    string Endpoint { get; }
    IConnectionMultiplexer Connection { get; set; }

    bool EnsureConnected();
}