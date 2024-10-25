// -----------------------------------------------------------------------
// <copyright file="QueueClientFactory.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Storage.Queues;

using System.Collections.Concurrent;
using Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class QueueClientFactory : IQueueClientFactory
{
    private readonly ILogger<QueueClientFactory> logger;
    private readonly IServiceProvider serviceProvider;
    private readonly ILoggerFactory loggerFactory;

    private readonly ConcurrentDictionary<string, object> _repositories = new ConcurrentDictionary<string, object>();

    public QueueClientFactory(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
    {
        this.serviceProvider = serviceProvider;
        this.loggerFactory = loggerFactory;
        logger = loggerFactory.CreateLogger<QueueClientFactory>();
    }

    public IQueueStorageClient<T> GetQueueClient<T>(string? settingName = null) where T : class, new()
    {
        if (_repositories.TryGetValue(typeof(T).Name, out var found) && found is IQueueStorageClient<T> client)
        {
            return client;
        }

        logger.CreatingQueueClient(typeof(T).FullName ?? "Unknown");
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        QueueSettings queueSettings = configuration.GetConfiguredSettings<QueueSettings>(settingName);

        logger.CreateQueueWithTypeClientStart(queueSettings.Account, queueSettings.AuthMode.ToString(), typeof(T).FullName ?? "unknown");
        var queueClient = new QueueStorageClient<T>(serviceProvider, loggerFactory, new OptionsWrapper<QueueSettings>(queueSettings));
        _repositories.AddOrUpdate(typeof(T).Name, queueClient, (k, v) => queueClient);
        return queueClient;
    }
}