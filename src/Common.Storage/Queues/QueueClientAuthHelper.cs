// -----------------------------------------------------------------------
// <copyright file="QueueClientAuthHelper.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Storage.Queues;

using System.Net;
using Auth;
using Azure.Identity;
using Azure.Storage.Queues;
using Config;
using KeyVault;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.Configuration;
using Settings;

public class QueueClientAuthHelper
{
    private readonly ILogger<QueueClientAuthHelper> logger;
    private readonly QueueSettings queueSettings;
    private readonly ISecretProvider? secretProvider;
    private readonly AadTokenProvider authHelper;

    public QueueClientAuthHelper(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, QueueSettings? queueSettingsFromFactory = null)
    {
        logger = loggerFactory.CreateLogger<QueueClientAuthHelper>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        queueSettings = queueSettingsFromFactory ?? configuration.GetConfiguredSettings<QueueSettings>();
        secretProvider = serviceProvider.GetService<ISecretProvider>();
        authHelper = new AadTokenProvider(serviceProvider);

        switch (queueSettings.AuthMode)
        {
            case StorageAuthMode.Msi:
                TryCreateClientUsingMsi();
                break;
            case StorageAuthMode.Spn:
                TryCreateClientUsingSpn();
                break;
            case StorageAuthMode.AuthKeySecretFromVault:
            case StorageAuthMode.ConnectionStringFromVault:
                TryCreateClientFromKeyVault();
                break;
            case StorageAuthMode.AuthKeyFromEnvironment:
            case StorageAuthMode.ConnectionStringFromEnvironment:
                TryCreateClientUsingConnStr();
                break;
            default:
                throw new NotSupportedException($"Storage auth mode: {queueSettings.AuthMode} is not supported");
        }
    }

    public QueueClient QueueClient { get; private set; }

    public QueueClient DeadLetterQueueClient { get; private set; }

    /// <summary>
    ///     running app/svc/pod/vm is assigned an identity (user-assigned, system-assigned).
    /// </summary>
    private void TryCreateClientUsingMsi()
    {
        logger.CreateQueueClientStart(queueSettings.Account, queueSettings.AuthMode.ToString());
        try
        {
            var queueServiceClient = new QueueServiceClient(queueSettings.AccountServiceUrl, new ManagedIdentityCredential());
            VerifyQueueServiceClient(queueServiceClient, queueSettings.QueueName);
            QueueClient = queueServiceClient.GetQueueClient(queueSettings.QueueName);
            DeadLetterQueueClient = queueServiceClient.GetQueueClient(queueSettings.DeadLetterQueueName);
            logger.CreateQueueClientStop(queueSettings.Account, queueSettings.AuthMode.ToString());
        }
        catch (Exception ex)
        {
            logger.CreateQueueClientFailed(queueSettings.Account, queueSettings.AuthMode.ToString(), ex.Message);
        }
    }

    /// <summary>
    ///     using pre-configured spn to access storage, secret must be provided for spn authentication
    /// </summary>
    private void TryCreateClientUsingSpn()
    {
        logger.CreateQueueClientStart(queueSettings.Account, queueSettings.AuthMode.ToString());
        try
        {
            var (clientCredential, _) = authHelper.GetClientCredential(CancellationToken.None);
            var queueServiceClient = new QueueServiceClient(queueSettings.AccountServiceUrl, clientCredential);

            VerifyQueueServiceClient(queueServiceClient, queueSettings.QueueName);

            QueueClient = queueServiceClient.GetQueueClient(queueSettings.QueueName);
            DeadLetterQueueClient = queueServiceClient.GetQueueClient(queueSettings.DeadLetterQueueName);
            using var availableQueues = queueServiceClient.GetQueues().GetEnumerator();
            while (availableQueues.MoveNext())
            {
                if (availableQueues.Current?.Name == queueSettings.QueueName)
                {
                    logger.CreateQueueClientStop(queueSettings.Account, queueSettings.AuthMode.ToString());
                    return;
                }
            }

            logger.QueueNotFound(queueSettings.Account, queueSettings.QueueName);
        }
        catch (Exception ex)
        {
            logger.CreateQueueClientFailed(queueSettings.Account, queueSettings.AuthMode.ToString(), ex.Message);
        }
    }

    /// <summary>
    ///     using pre-configured spn to access key vault, then retrieve sas/conn string for storage
    /// </summary>
    private void TryCreateClientFromKeyVault()
    {
        if (string.IsNullOrEmpty(queueSettings.ConnectionName))
        {
            throw new InvalidOperationException($"Missing connection name for storage queue: {queueSettings.ConnectionName}");
        }

        if (secretProvider == null)
        {
            throw new InvalidConfigurationException(
                $"QueueClientAuthHelper: SecretProvider is not registered for queue client {queueSettings.Account} with auth mode: {queueSettings.AuthMode}");
        }

        logger.CreateQueueClientStart(queueSettings.Account, queueSettings.AuthMode.ToString());
        try
        {
            var connStrSecret = secretProvider.GetSecret(queueSettings.ConnectionName);
            var storageConnection = connStrSecret;
            if (queueSettings.AuthMode == StorageAuthMode.AuthKeySecretFromVault)
            {
                storageConnection =
                    $"DefaultEndpointsProtocol=https;AccountName={queueSettings.Account};AccountKey={storageConnection};EndpointSuffix=core.windows.net";
            }

            var queueServiceClient = new QueueServiceClient(storageConnection, new QueueClientOptions());
            VerifyQueueServiceClient(queueServiceClient, queueSettings.QueueName);

            QueueClient = queueServiceClient.GetQueueClient(queueSettings.QueueName);
            DeadLetterQueueClient = queueServiceClient.GetQueueClient(queueSettings.DeadLetterQueueName);
            logger.CreateQueueClientStop(queueSettings.Account, queueSettings.AuthMode.ToString());
        }
        catch (Exception ex)
        {
            logger.CreateQueueClientFailed(queueSettings.Account, queueSettings.AuthMode.ToString(), ex.Message);
            throw;
        }
    }

    /// <summary>
    ///     connection string is provided as env variable (most unsecure).
    /// </summary>
    private void TryCreateClientUsingConnStr()
    {
        logger.CreateQueueClientStart(queueSettings.Account, queueSettings.AuthMode.ToString());
        if (string.IsNullOrEmpty(queueSettings.ConnectionName) || string.IsNullOrEmpty(Environment.GetEnvironmentVariable(queueSettings.ConnectionName)))
        {
            throw new InvalidOperationException($"Missing environment name or variable for storage connection: {queueSettings.ConnectionName}");
        }

        try
        {
            var storageConnection = Environment.GetEnvironmentVariable(queueSettings.ConnectionName);
            if (queueSettings.AuthMode == StorageAuthMode.AuthKeyFromEnvironment)
            {
                storageConnection =
                    $"DefaultEndpointsProtocol=https;AccountName={queueSettings.Account};AccountKey={storageConnection};EndpointSuffix=core.windows.net";
            }

            var queueServiceClient = new QueueServiceClient(storageConnection, new QueueClientOptions());
            VerifyQueueServiceClient(queueServiceClient, queueSettings.QueueName);

            QueueClient = queueServiceClient.GetQueueClient(queueSettings.QueueName);
            DeadLetterQueueClient = queueServiceClient.GetQueueClient(queueSettings.DeadLetterQueueName);
            logger.CreateQueueClientStop(queueSettings.Account, queueSettings.AuthMode.ToString());
        }
        catch (Exception ex)
        {
            logger.CreateQueueClientFailed(queueSettings.Account, queueSettings.AuthMode.ToString(), ex.Message);
            throw;
        }
    }

    private void VerifyQueueServiceClient(QueueServiceClient queueServiceClient, string queueName)
    {
        try
        {
            using var availableQueues = queueServiceClient.GetQueues().GetEnumerator();
            var foundQueue = false;
            while (availableQueues.MoveNext())
            {
                if (availableQueues.Current?.Name == queueName)
                {
                    foundQueue = true;
                    logger.CreateQueueClientStop(queueSettings.Account, queueSettings.AuthMode.ToString());
                    break;
                }
            }

            if (!foundQueue)
            {
                var error = $"Unable to find queue with name {queueSettings.QueueName}";
                logger.QueueNotFoundError(queueSettings.QueueName, error);
                throw new InvalidOperationException(error);
            }

            var queueClientToTest = queueServiceClient.GetQueueClient(queueName);
            var properties = queueClientToTest.GetProperties();
            var queueLength = properties.Value.ApproximateMessagesCount;
            logger.ReportQueueLength(queueClientToTest.Name, queueLength);
            var fetchPropStatus = properties.GetRawResponse().Status;
            if (!IsSuccessStatusCode(fetchPropStatus))
            {
                throw new InvalidOperationException("Failed to fetch queue properties");
            }

            var testQueueClient = queueServiceClient.GetQueueClient("test");
            var createQueueResponse = testQueueClient.Create();
            if (!IsSuccessStatusCode(createQueueResponse.Status))
            {
                throw new InvalidOperationException("Failed to create test queue");
            }

            var sendMsgResponse = testQueueClient.SendMessage("test");
            var statusCode = sendMsgResponse.GetRawResponse().Status;
            if (!IsSuccessStatusCode(statusCode))
            {
                throw new InvalidOperationException("Failed to send message to test queue");
            }
        }
        catch (Exception ex)
        {
            logger.ConnectToQueueError(queueSettings.QueueName, ex.Message);
            throw;
        }
    }

    private bool IsSuccessStatusCode(int statusCode)
    {
        return statusCode == (int)HttpStatusCode.OK ||
               statusCode == (int)HttpStatusCode.Created ||
               statusCode == (int)HttpStatusCode.Accepted ||
               statusCode == (int)HttpStatusCode.NoContent;
    }
}