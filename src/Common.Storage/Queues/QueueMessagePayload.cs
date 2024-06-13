// -----------------------------------------------------------------------
// <copyright file="QueueMessagePayload.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Storage.Queues;

public class QueueMessagePayload
{
    public string MessageId { get; set; }
    public string MessageText { get; set; }
    public string PopReceipt { get; set; }
}