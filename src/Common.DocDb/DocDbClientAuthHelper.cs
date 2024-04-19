// -----------------------------------------------------------------------
// <copyright file="DocDbClientAuthHelper.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.DocDb;

using System;
using System.IO;
using Azure.Identity;
using Config;
using KeyVault;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.Configuration;
using Settings;

public class DocDbClientAuthHelper
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<DocDbClientAuthHelper> logger;
    private readonly IConfiguration configuration;
    private readonly DocDbSettings docDbSettings;

    private readonly CosmosSerializationOptions serializerOptions = new CosmosSerializationOptions
    {
        IgnoreNullValues = true,
        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
    };

    public DocDbClientAuthHelper(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, DocDbSettings? settings = null)
    {
        this.serviceProvider = serviceProvider;
        logger = loggerFactory.CreateLogger<DocDbClientAuthHelper>();
        configuration = serviceProvider.GetRequiredService<IConfiguration>();
        docDbSettings = settings ?? configuration.GetConfiguredSettings<DocDbSettings>();
    }

    public CosmosClient GetClient()
    {
        return docDbSettings.AuthMode switch
        {
            DocDbAuthMode.Msi => GetClientUsingAuthKeyWithMsi(),
            DocDbAuthMode.AuthKeyFromKeyVault => GetClientFromKeyVault(),
            DocDbAuthMode.AuthKeyFromEnvironment => GetClientUsingAuthKeyFromEnvironment(),
            DocDbAuthMode.ConnectionStringFromEnvironment => GetClientUsingConnectionStringFromEnvironment(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private CosmosClient GetClientFromKeyVault()
    {
        var vaultSettings = configuration.GetConfiguredSettings<VaultSettings>();
        var secretProvider = serviceProvider.GetService<ISecretProvider>();
        if (secretProvider == null)
        {
            throw new InvalidConfigurationException($"SecretProvider is not registered for cosmos client {docDbSettings.Account} with auth mode: {docDbSettings.AuthMode}");
        }

        logger.ReadSecretFromKeyVaultStart(docDbSettings.AuthKeySecret, vaultSettings.VaultName);
        var authKey = secretProvider.GetSecret(docDbSettings.AuthKeySecret);
        logger.ReadSecretFromKeyVaultStop(docDbSettings.AuthKeySecret, vaultSettings.VaultName);
        return new CosmosClientBuilder(docDbSettings.AccountUri.ToString(), authKey)
            .WithConsistencyLevel(Microsoft.Azure.Cosmos.ConsistencyLevel.Session)
            .WithConnectionModeDirect()
            .WithSerializerOptions(serializerOptions)
            .Build();
    }

    private CosmosClient GetClientUsingAuthKeyWithMsi()
    {
        logger.CreateCosmosClientUsing(docDbSettings.Account, "MSI");
        var cosmosClient = new CosmosClientBuilder(docDbSettings.AccountUri.ToString(), tokenCredential: new DefaultAzureCredential())
            .WithConsistencyLevel(Microsoft.Azure.Cosmos.ConsistencyLevel.Session)
            .WithConnectionModeDirect()
            .WithSerializerOptions(serializerOptions)
            .Build();
        return cosmosClient;
    }

    private CosmosClient GetClientUsingAuthKeyFromEnvironment()
    {
        logger.ReadSecretFromEnvironmentStart(docDbSettings.AuthKeySecret);
        var authKey = Environment.GetEnvironmentVariable(docDbSettings.AuthKeySecret);
        if (string.IsNullOrEmpty(authKey))
        {
            var homeFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var secretFilePath = Path.Combine(homeFolder, ".secrets", docDbSettings.AuthKeySecret);
            if (File.Exists(secretFilePath))
            {
                authKey = File.ReadAllText(secretFilePath).Trim();
            }
        }

        if (string.IsNullOrEmpty(authKey))
        {
            logger.ReadSecretFromEnvironmentFailed(docDbSettings.AuthKeySecret);
            throw new InvalidOperationException($"Failed to retrieve secret {docDbSettings.AuthKeySecret} from environment");
        }

        logger.ReadSecretFromEnvironmentStop(docDbSettings.AuthKeySecret);
        return new CosmosClientBuilder(docDbSettings.AccountUri.ToString(), authKey)
            .WithConsistencyLevel(Microsoft.Azure.Cosmos.ConsistencyLevel.Session)
            .WithConnectionModeDirect()
            .WithSerializerOptions(serializerOptions)
            .Build();
    }

    private CosmosClient GetClientUsingConnectionStringFromEnvironment()
    {
        logger.ReadSecretFromEnvironmentStart(docDbSettings.AuthKeySecret);
        var connectionString = Environment.GetEnvironmentVariable(docDbSettings.AuthKeySecret);
        if (string.IsNullOrEmpty(connectionString))
        {
            var homeFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var secretFilePath = Path.Combine(homeFolder, ".secrets", docDbSettings.AuthKeySecret);
            if (File.Exists(secretFilePath))
            {
                connectionString = File.ReadAllText(secretFilePath).Trim();
            }
        }

        if (string.IsNullOrEmpty(connectionString))
        {
            logger.ReadSecretFromEnvironmentFailed(docDbSettings.AuthKeySecret);
            throw new InvalidOperationException($"Failed to retrieve secret {docDbSettings.AuthKeySecret} from environment");
        }

        logger.ReadSecretFromEnvironmentStop(docDbSettings.AuthKeySecret);
        return new CosmosClientBuilder(connectionString)
            .WithConsistencyLevel(Microsoft.Azure.Cosmos.ConsistencyLevel.Session)
            .WithConnectionModeDirect()
            .WithSerializerOptions(serializerOptions)
            .Build();
    }
}