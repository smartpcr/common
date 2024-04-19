// -----------------------------------------------------------------------
// <copyright file="BlobInfo.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Storage.Blobs;

public class BlobInfo
{
    public string Name { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public bool IsLeased { get; set; }
    public TimeSpan TimeToLive { get; set; }
    public long Size { get; set; }
}