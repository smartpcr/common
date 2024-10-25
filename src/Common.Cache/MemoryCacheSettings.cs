// -----------------------------------------------------------------------
// <copyright file="MemoryCacheSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Cache;

public class MemoryCacheSettings
{
    public double CompactionPercentage { get; set; } = 0.1;

    /// <summary>
    ///     max memory size in MB
    /// </summary>
    public int SizeLimit { get; set; } = 1024;
}