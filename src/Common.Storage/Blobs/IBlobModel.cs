// -----------------------------------------------------------------------
// <copyright file="IBlobModel.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Storage.Blobs;

public interface IBlobModel
{
    string ETag { get; set; }
}