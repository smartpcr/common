// -----------------------------------------------------------------------
// <copyright file="VhdValidator.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Storage.Blobs;

using System.Net;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.Storage.Blob;

public static class VhdValidator
{
    /// <summary>
    /// Adds the VHD footer to the page blob.
    /// </summary>
    /// <param name="pageBlobClient">The page blob client.</param>
    /// <param name="size">Current size in bytes.</param>
    /// <param name="diskType">The <see cref="DiskFormatType"/>.</param>
    /// <param name="cancel">The cancellation token.</param>
    public static async Task UploadVhdFooter(PageBlobClient pageBlobClient, long size, DiskFormatType diskType, CancellationToken cancel)
    {
        var footer = CreateVhdFooter(size, diskType);
        using var ms = new MemoryStream(footer);
        await pageBlobClient.UploadPagesAsync(ms, size - VhdConstants.VHD_FOOTER_SIZE, null, cancel);
    }

    public static async Task<bool> ValidateVhdChecksum(CloudPageBlob pageBlob, CancellationToken cancel)
    {
        byte[] vhdFooterBytes = await FetchVhdFooterAsync(pageBlob, cancel);
        var vhdFooterBeforeChecksumStartSplit = vhdFooterBytes.Take(VhdConstants.CHECKSUM_OFFSET);
        var vhdFooterAfterChecksumStartSplit = vhdFooterBytes.Skip(VhdConstants.CHECKSUM_OFFSET).ToArray();
        var vhdChecksumBytes = vhdFooterAfterChecksumStartSplit.Take(sizeof(int)).ToArray();
        var vhdChecksumValue = vhdChecksumBytes[0] << 24 | vhdChecksumBytes[1] << 16 | vhdChecksumBytes[2] << 8 | vhdChecksumBytes[3];

        var checkSum = 0;
        foreach (var byteToSum in vhdFooterBeforeChecksumStartSplit.Concat(vhdFooterAfterChecksumStartSplit.Skip(sizeof(int))))
        {
            checkSum += byteToSum;
        }

        return vhdChecksumValue == ~checkSum;
    }

    public static async Task<DiskFormatType> GetDiskFormatType(CloudPageBlob pageBlob, CancellationToken cancel)
    {
        byte[] vhdFooterBytes = await FetchVhdFooterAsync(pageBlob, cancel);
        var diskFormatTypeBytes = vhdFooterBytes.Skip(VhdConstants.DISK_TYPE_OFFSET).Take(sizeof(int)).ToArray();
        var diskFormatType = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(diskFormatTypeBytes, 0)); // big endian
        return (DiskFormatType)diskFormatType;
    }

    /// <summary>
    /// Vhd footer is 512 bytes long.
    /// 0-7: Cookie, 8 bytes
    /// 8-11: Features, 4 bytes
    /// 12-15: File Format Version, 4 bytes
    /// 16-23: Data Offset, 8 bytes, must be 0xFFFFFFFFFFFFFFFF for fixed disks
    /// 24-27: Time Stamp, 4 bytes
    /// 28-31: Creator Application, 4 bytes, must be "vpc " (0x20, 0x63, 0x70, 0x76) for Microsoft Virtual PC
    /// 32-35: Creator Version, 4 bytes
    /// 36-39: Creator Host OS, 4 bytes, e.g. 0x5769326B (Wi2k) for Windows
    /// 40-47: Original Size, 8 bytes
    /// 48-55: Current Size, 8 bytes
    /// 56-59: Disk Geometry, 4 bytes, i.e. Cylinders, Heads, Sectors per track
    /// 60-63: Disk Type, 4 bytes, i.e. Fixed, Dynamic, Differencing
    /// 64-67: Checksum, 4 bytes
    /// 68-83: Unique Id, 16 bytes
    /// 84-84: Saved State, 1 byte
    /// 85-511: Reserved, 427 bytes
    /// Size of disk is stored at offset 48 in the footer in bigEndian format.
    /// The disk type is stored at offset 60 in the footer as fixed (2).
    /// The checksum is calculated by adding all the bytes in the footer and stored at offset 64 in the footer.
    /// </summary>
    /// <param name="vhdSizeInBytes">Original size of disk in bytes, must be chunks of 512 bytes</param>
    /// <param name="diskType">The <see cref="DiskFormatType"/></param>
    /// <returns>Byte array representing vhd footer.</returns>
    private static byte[] CreateVhdFooter(long vhdSizeInBytes, DiskFormatType diskType)
    {
        byte[] footer = new byte[VhdConstants.VHD_FOOTER_SIZE];
        // Initialize with dummy data or real VHD footer data as required
        // Make sure to set the checksum bytes to zero before calculation
        Array.Clear(footer, VhdConstants.CHECKSUM_OFFSET, sizeof(int));

        // Convert disk size to bytes array (ensure Big Endian format)
        byte[] sizeBytes = BitConverter.GetBytes(vhdSizeInBytes);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(sizeBytes);
        }

        // Set the size in the appropriate place in the footer
        sizeBytes.CopyTo(footer, VhdConstants.CURRENT_SIZE_OFFSET);

        // Set the disk type to fixed
        var diskTypeBytes = BitConverter.GetBytes((int)diskType);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(diskTypeBytes);
        }
        diskTypeBytes.CopyTo(footer, VhdConstants.DISK_TYPE_OFFSET);

        // Calculate checksum
        int checksum = 0;
        foreach (byte b in footer)
        {
            checksum += b;
        }
        checksum = ~checksum;

        // Place the checksum into the footer
        footer[VhdConstants.CHECKSUM_OFFSET] = (byte)((checksum >> 24) & 0xFF);
        footer[VhdConstants.CHECKSUM_OFFSET + 1] = (byte)((checksum >> 16) & 0xFF);
        footer[VhdConstants.CHECKSUM_OFFSET + 2] = (byte)((checksum >> 8) & 0xFF);
        footer[VhdConstants.CHECKSUM_OFFSET + 3] = (byte)(checksum & 0xFF);

        return footer;
    }

    private static async Task<byte[]> FetchVhdFooterAsync(CloudPageBlob pageBlob, CancellationToken cancel)
    {
        await pageBlob.FetchAttributesAsync(cancel);
        long blobSize = pageBlob.Properties.Length;
        long footerOffset = blobSize - VhdConstants.VHD_FOOTER_SIZE;
        byte[] footer = new byte[VhdConstants.VHD_FOOTER_SIZE];
        using var ms = new MemoryStream(footer);
        await pageBlob.DownloadRangeToStreamAsync(ms, footerOffset, VhdConstants.VHD_FOOTER_SIZE, cancel);
        return footer;
    }
}