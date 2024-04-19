// -----------------------------------------------------------------------
// <copyright file="MessageFromQueue.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Storage.Queues;

public class MessageFromQueue<T>
{
    public T Value { get; set; }
    public string MessageId { get; set; }
    public string Receipt { get; set; }
    public int DequeueCount { get; set; }
}