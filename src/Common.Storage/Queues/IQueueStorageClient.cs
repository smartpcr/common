// -----------------------------------------------------------------------
// <copyright file="IQueueStorageClient.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Storage.Queues;

public interface IQueueStorageClient<T>
{
    Task<MessageReceipt> EnqueueAsync(T message, CancellationToken cancellationToken);

    Task<List<MessageFromQueue<T>>> DequeueAsync(int maxMessages, TimeSpan visibilityTimeout, CancellationToken cancellationToken);

    Task<IEnumerable<QueueMessagePayload>> DequeueMessagesAsync(int maxMessages, TimeSpan visibilityTimeout, CancellationToken cancellationToken);

    Task<bool> PeekAsync(CancellationToken cancellationToken);
    Task ResetVisibilityAsync(string messageId, string receipt, T message, CancellationToken cancellationToken);
    Task<int> GetQueueLengthAsync(CancellationToken cancellationToken);
    Task DeleteMessageAsync(string messageId, string receipt, CancellationToken cancellationToken);
}