// -----------------------------------------------------------------------
// <copyright file="IQueueClientFactory.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Storage.Queues;

public interface IQueueClientFactory
{
    IQueueStorageClient<T> GetQueueClient<T>(string? settingName = null) where T : class, new();
}