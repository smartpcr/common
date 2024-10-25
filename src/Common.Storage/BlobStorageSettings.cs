// -----------------------------------------------------------------------
// <copyright file="BlobStorageSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Storage;

using System.ComponentModel.DataAnnotations;

public class BlobStorageSettings
{
    /// <summary>
    /// Gets or sets the account name for azure storage.
    /// </summary>
    [Required]
    public string Account { get; set; }

    /// <summary>
    /// Gets or sets container name in Blob Storage.
    /// </summary>
    /// <remarks>
    /// The <see cref="Container"/> property represents the name of the container in Blob Storage.
    /// </remarks>
    public string Container { get; set; }

    /// <summary>
    /// Gets or sets the connection name for accessing the blob storage. This is either for authkey or connection string
    /// </summary>
    /// <remarks>
    /// This property is used in the <c>BlobClientAuthHelper</c> class to retrieve the storage connection string or SAS token from a key vault. The value of this property should match the
    /// name of the secret in the key vault.
    /// </remarks>
    /// <value>The connection name for accessing the blob storage.</value>
    public string ConnectionName { get; set; }

    /// <summary>
    /// Gets the endpoint URL for the container.
    /// </summary>
    public string ContainerEndpoint => $"https://{Account}.blob.core.windows.net/{Container}";

    /// <summary>
    /// Gets or sets the authentication mode for accessing the blob storage.
    /// </summary>
    public StorageAuthMode AuthMode { get; set; } = StorageAuthMode.Msi;
}