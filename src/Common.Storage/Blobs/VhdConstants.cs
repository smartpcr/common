// -----------------------------------------------------------------------
// <copyright file="VhdConstants.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Storage.Blobs;

public class VhdConstants
{
    public const long VHD_DEFAULT_BLOCK_SIZE = 524288;
    public const long VHD_NO_DATA_LONG = -1;
    public const uint VHD_NO_DATA_INT = 4294967295;
    public const long VHD_PAGE_SIZE = 512;
    public const int VHD_FOOTER_SIZE = 512;
    public const int VHD_SECTOR_LENGTH = 512;
    public const int VHD_FOOTER_OFFSET_CHECKSUM = 64;
    public const int VHD_PREFETCH_FOOTER_SIZE = 512;
    public const int VhdCustomExtensionFooterOffsetFromEnd = 1024;
    public const int VhdDiskFormatTypeOffsetFromEnd = 452;
    public const int ORIGINAL_SIZE_OFFSET = 40; // 8 bytes
    public const int CURRENT_SIZE_OFFSET = 48; // 8 bytes
    public const int DISK_TYPE_OFFSET = 60; // 4 bytes
    public const int CHECKSUM_OFFSET = 64; // 4 bytes
}

public enum DiskFormatType
{
    /// <summary>
    /// No type found
    /// </summary>
    None = 0,

    // Reserved = 1
    // This is deprecated in the Vhd Format spec

    /// <summary>
    /// The fixed
    /// </summary>
    Fixed = 2,

    /// <summary>
    /// The dynamic
    /// </summary>
    Dynamic = 3,

    /// <summary>
    /// The differencing
    /// </summary>
    Differencing = 4
}