// -----------------------------------------------------------------------
// <copyright file="QueueStorageClient.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Storage.Queues;

using Azure.Storage.Queues;
using Common.Config;
using Common.Settings;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenTelemetry.Trace;

public class QueueStorageClient<T> : IQueueStorageClient<T> where T : class, new()
{
    private readonly QueueClient client;
    private readonly QueueClient deadLetterQueueClient;
    private readonly ILogger<QueueStorageClient<T>> logger;
    private readonly Tracer tracer;
    private readonly QueueSettings queueSettings;

    public QueueStorageClient(
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory,
        IOptions<QueueSettings>? queueSettingsOptions = null)
    {
        logger = loggerFactory.CreateLogger<QueueStorageClient<T>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var metadata = configuration.GetConfiguredSettings<ApplicationMetadata>();
        var traceProvider = serviceProvider.GetRequiredService<TracerProvider>();
        tracer = traceProvider.GetTracer(metadata.ApplicationName + $".{nameof(QueueClient)}", metadata.BuildVersion);
        queueSettings = queueSettingsOptions?.Value ?? configuration.GetConfiguredSettings<QueueSettings>();

        var clientFactory = new QueueClientAuthHelper(serviceProvider, loggerFactory, queueSettings);
        client = clientFactory.QueueClient;
        deadLetterQueueClient = clientFactory.DeadLetterQueueClient;
    }

    public async Task<MessageReceipt> EnqueueAsync(T message, CancellationToken cancellationToken)
    {
        using var _ = tracer.StartActiveSpan(nameof(EnqueueAsync));
        var queueMessage = JsonConvert.SerializeObject(message);
        var receipt = await client.SendMessageAsync(queueMessage, cancellationToken);
        return new MessageReceipt { MessageId = receipt.Value.MessageId };
    }

    public async Task<List<MessageFromQueue<T>>> DequeueAsync(
        int maxMessages,
        TimeSpan visibilityTimeout,
        CancellationToken cancellationToken)
    {
        using var _ = tracer.StartActiveSpan(nameof(DequeueAsync));
        var response = await client.ReceiveMessagesAsync(maxMessages, visibilityTimeout, cancellationToken);
        var messages = response.Value;
        var messageList = messages.Select(m => new MessageFromQueue<T>
        {
            Value = JsonConvert.DeserializeObject<T>(m.MessageText)!,
            Receipt = m.PopReceipt,
            MessageId = m.MessageId,
            DequeueCount = (int)m.DequeueCount
        }).ToList();
        logger.DequeueStop(messageList.Count, queueSettings.QueueName);

        var messagesToRemove = messageList.Where(m => m.DequeueCount >= queueSettings.MaxDequeueCount).ToList();
        if (messagesToRemove.Count > 0)
        {
            foreach (var deadMsg in messagesToRemove)
            {
                var jsonMsg = JsonConvert.SerializeObject(deadMsg.Value);
                await deadLetterQueueClient.SendMessageAsync(jsonMsg, cancellationToken);
                messageList.Remove(deadMsg);
                logger.MoveToDeadLetterQueue(queueSettings.DeadLetterQueueName, deadMsg.DequeueCount, queueSettings.MaxDequeueCount);
            }
        }

        return messageList;
    }

    public async Task<IEnumerable<QueueMessagePayload>> DequeueMessagesAsync(int maxMessages, TimeSpan visibilityTimeout, CancellationToken cancellationToken)
    {
        using var _ = tracer.StartActiveSpan(nameof(DequeueMessagesAsync));
        var response = await client.ReceiveMessagesAsync(maxMessages, visibilityTimeout, cancellationToken);
        logger.DequeueStop(response.Value.Length, queueSettings.QueueName);
        return response.Value.Select(v => new QueueMessagePayload { MessageText = v.MessageText });
    }

    public async Task<bool> PeekAsync(CancellationToken cancellationToken)
    {
        using var _ = tracer.StartActiveSpan(nameof(PeekAsync));
        var peekResponse = await client.PeekMessagesAsync(cancellationToken: cancellationToken);
        return peekResponse.Value.Any();
    }

    public async Task ResetVisibilityAsync(string messageId, string receipt, T message, CancellationToken cancellationToken)
    {
        using var _ = tracer.StartActiveSpan(nameof(ResetVisibilityAsync));
        var messageJson = JsonConvert.SerializeObject(message);
        await client.UpdateMessageAsync(
            messageId,
            receipt,
            messageJson,
            TimeSpan.FromSeconds(10),
            cancellationToken);
    }

    public async Task<int> GetQueueLengthAsync(CancellationToken cancellationToken)
    {
        using var _ = tracer.StartActiveSpan(nameof(GetQueueLengthAsync));
        var props = await client.GetPropertiesAsync(cancellationToken);
        return props.Value.ApproximateMessagesCount;
    }

    public async Task DeleteMessageAsync(string messageId, string receipt, CancellationToken cancellationToken)
    {
        using var _ = tracer.StartActiveSpan(nameof(DeleteMessageAsync));
        await client.DeleteMessageAsync(messageId, receipt, cancellationToken);
    }
}