// -----------------------------------------------------------------------
// <copyright file="QueueSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Storage.Queues;

using System;
using System.ComponentModel.DataAnnotations;

public class QueueSettings
{
    [Required]
    public string Account { get; set; }
    [Required]
    public string QueueName { get; set; }
    public string ConnectionName { get; set; }
    public int MaxDequeueCount { get; set; }
    public string DeadLetterQueueName { get; set; }
    public Uri AccountServiceUrl => new Uri($"https://{Account}.queue.core.windows.net");
    public StorageAuthMode AuthMode { get; set; } = StorageAuthMode.Msi;
}