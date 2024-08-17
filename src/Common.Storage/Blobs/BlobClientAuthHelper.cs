// -----------------------------------------------------------------------
// <copyright file="BlobClientAuthHelper.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Storage.Blobs;

using System.Text;
using Auth;
using Azure.Identity;
using Azure.Storage.Blobs;
using Config;
using KeyVault;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.Configuration;
using Newtonsoft.Json;
using Settings;

public class BlobClientAuthHelper
{
    private readonly BlobStorageSettings blobSettings;
    private readonly ILogger<BlobClientAuthHelper> logger;
    private readonly ISecretProvider? secretProvider;
    private readonly AadTokenProvider? authTokenProvider;

    public BlobServiceClient BlobService { get; private set; }
    public BlobContainerClient ContainerClient { get; private set; }
    public Func<string, BlobContainerClient> CreateContainerClient { get; private set; }

    public BlobClientAuthHelper(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, BlobStorageSettings? settings = null)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        blobSettings = settings ?? configuration.GetConfiguredSettings<BlobStorageSettings>();
        secretProvider = serviceProvider.GetService<ISecretProvider>();
        authTokenProvider = configuration.ContainsSettings<AadSettings>()
            ? new AadTokenProvider(serviceProvider)
            : null;
        logger = loggerFactory.CreateLogger<BlobClientAuthHelper>();

        (BlobService, ContainerClient, CreateContainerClient) = Initialize();
    }

    private (
        BlobServiceClient blobServiceClient,
        BlobContainerClient containerClient,
        Func<string, BlobContainerClient> createContainerClient) Initialize()
    {
        (BlobServiceClient blobServiceClient,
            BlobContainerClient containerClient,
            Func<string, BlobContainerClient> createContainerClient) output =
            blobSettings.AuthMode switch
            {
                StorageAuthMode.Msi => TryCreateUsingMsi(),
                StorageAuthMode.Spn => TryCreateUsingSpn(),
                StorageAuthMode.AuthKeySecretFromVault => TryCreateFromKeyVault(),
                StorageAuthMode.ConnectionStringFromVault => TryCreateFromKeyVault(),
                StorageAuthMode.AuthKeyFromEnvironment => TryCreateUsingEnvVar(),
                StorageAuthMode.ConnectionStringFromEnvironment => TryCreateUsingEnvVar(),
                _ => throw new NotSupportedException($"Storage auth mode: {blobSettings.AuthMode} is not supported")
            };

        return output;
    }

    /// <summary>
    ///     running app/svc/pod/vm is assigned an identity (user-assigned, system-assigned)
    /// </summary>
    /// <returns>instance of <see cref="BlobServiceClient"/> and <see cref="BlobContainerClient"/>, as well as factory method for <see cref="BlobContainerClient"/></returns>
    private (
            BlobServiceClient blobServiceClient,
            BlobContainerClient containerClient,
            Func<string, BlobContainerClient> createContainerClient)
        TryCreateUsingMsi()
    {
        logger.CreateBlobClientStart(blobSettings.Account, blobSettings.AuthMode.ToString());
        try
        {
            var containerClient = new BlobContainerClient(
                new Uri(blobSettings.ContainerEndpoint),
                new ManagedIdentityCredential());
            containerClient.CreateIfNotExists();

            TryRecreateTestBlob(containerClient);
            logger.CreateBlobClientStop(blobSettings.Account, blobSettings.AuthMode.ToString());
            var blobServiceClient = new BlobServiceClient(
                new Uri($"https://{blobSettings.Account}.blob.core.windows.net/"),
                new ManagedIdentityCredential());
            Func<string, BlobContainerClient> createContainerClient = name =>
                new BlobContainerClient(
                    new Uri($"https://{blobSettings.Account}.blob.core.windows.net/{name}"),
                    new ManagedIdentityCredential());

            return (blobServiceClient, containerClient, createContainerClient);
        }
        catch (Exception ex)
        {
            logger.AccessBlobError(blobSettings.Account, blobSettings.Container, blobSettings.AuthMode.ToString(), ex.Message);
            throw;
        }
    }

    /// <summary>
    ///     using pre-configured spn to access storage, secret must be provided for spn authentication
    /// </summary>
    /// <returns>instance of <see cref="BlobServiceClient"/> and <see cref="BlobContainerClient"/>, as well as factory method for <see cref="BlobContainerClient"/></returns>
    private (
            BlobServiceClient blobServiceClient,
            BlobContainerClient containerClient,
            Func<string, BlobContainerClient> createContainerClient)
        TryCreateUsingSpn()
    {
        logger.CreateBlobClientStart(blobSettings.Account, blobSettings.AuthMode.ToString());
        if (this.authTokenProvider == null)
        {
            throw new InvalidConfigurationException($"Blob client auth {blobSettings.AuthMode} requires {nameof(AadSettings)} to be configured");
        }

        try
        {
            var (clientCredential, _) = authTokenProvider.GetClientCredential(CancellationToken.None);

            BlobContainerClient containerClient = new BlobContainerClient(
                new Uri(blobSettings.ContainerEndpoint),
                clientCredential);
            BlobServiceClient blobServiceClient = new BlobServiceClient(
                new Uri($"https://{blobSettings.Account}.blob.core.windows.net/"),
                clientCredential);
            Func<string, BlobContainerClient> createContainerClient = name =>
                new BlobContainerClient(
                    new Uri($"https://{blobSettings.Account}.blob.core.windows.net/{name}"),
                    clientCredential);

            TryRecreateTestBlob(containerClient);
            logger.CreateBlobClientStop(blobSettings.Account, blobSettings.AuthMode.ToString());

            return (blobServiceClient, containerClient, createContainerClient);
        }
        catch (Exception ex)
        {
            logger.CreateBlobClientFailed(blobSettings.Account, blobSettings.AuthMode.ToString(), ex.Message);
            throw;
        }
    }

    /// <summary>
    ///     using pre-configured spn to access key vault, then retrieve sas/conn string for storage
    /// </summary>
    /// <returns>instance of <see cref="BlobServiceClient"/> and <see cref="BlobContainerClient"/>, as well as factory method for <see cref="BlobContainerClient"/></returns>
    private (
            BlobServiceClient blobServiceClient,
            BlobContainerClient containerClient,
            Func<string, BlobContainerClient> createContainerClient)
        TryCreateFromKeyVault()
    {
        if (string.IsNullOrEmpty(blobSettings.ConnectionName))
        {
            throw new InvalidConfigurationException($"Missing connection name for storage: {blobSettings.ConnectionName}");
        }

        if (secretProvider == null)
        {
            throw new InvalidConfigurationException($"Blob client auth {blobSettings.AuthMode} requires secret provider");
        }

        logger.CreateBlobClientStart(blobSettings.Account, blobSettings.AuthMode.ToString());
        try
        {
            var connStrSecret = secretProvider.GetSecret(blobSettings.ConnectionName);
            var storageConnection = connStrSecret;
            if (blobSettings.AuthMode == StorageAuthMode.AuthKeySecretFromVault)
            {
                storageConnection =
                    $"DefaultEndpointsProtocol=https;AccountName={blobSettings.Account};AccountKey={storageConnection};EndpointSuffix=core.windows.net";
            }

            var containerClient = new BlobContainerClient(storageConnection, blobSettings.Container);
            containerClient.CreateIfNotExists();

            TryRecreateTestBlob(containerClient);
            logger.CreateBlobClientStop(blobSettings.Account, blobSettings.AuthMode.ToString());
            var blobServiceClient = new BlobServiceClient(storageConnection);
            Func<string, BlobContainerClient> createContainerClient = name => new BlobContainerClient(storageConnection, name);
            return (blobServiceClient, containerClient, createContainerClient);
        }
        catch (Exception ex)
        {
            logger.AccessBlobError(blobSettings.Account, blobSettings.Container, blobSettings.AuthMode.ToString(), ex.Message);
            throw;
        }
    }

    /// <summary>
    ///     connection string is provided as env variable (most unsecure)
    /// </summary>
    /// <returns>instance of <see cref="BlobServiceClient"/> and <see cref="BlobContainerClient"/>, as well as factory method for <see cref="BlobContainerClient"/></returns>
    private (
            BlobServiceClient blobServiceClient,
            BlobContainerClient containerClient,
            Func<string, BlobContainerClient> createContainerClient)
        TryCreateUsingEnvVar()
    {
        if (string.IsNullOrEmpty(blobSettings.ConnectionName))
        {
            throw new InvalidOperationException($"Missing setting for storage connection: {blobSettings.ConnectionName}");
        }

        logger.CreateBlobClientStart(blobSettings.Account, blobSettings.AuthMode.ToString());
        try
        {
            var storageConnection = Environment.GetEnvironmentVariable(blobSettings.ConnectionName);
            if (string.IsNullOrEmpty(storageConnection))
            {
                var homeFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var secretFilePath = Path.Combine(homeFolder, ".secrets", blobSettings.ConnectionName);
                if (File.Exists(secretFilePath))
                {
                    storageConnection = File.ReadAllText(secretFilePath).Trim();
                }
            }

            if (string.IsNullOrEmpty(storageConnection))
            {
                throw new InvalidOperationException($"Failed to retrieve secret {blobSettings.ConnectionName} from environment");
            }

            if (blobSettings.AuthMode == StorageAuthMode.AuthKeyFromEnvironment)
            {
                storageConnection =
                    $"DefaultEndpointsProtocol=https;AccountName={blobSettings.Account};AccountKey={storageConnection};EndpointSuffix=core.windows.net";
            }

            var containerClient = new BlobContainerClient(storageConnection, blobSettings.Container);
            containerClient.CreateIfNotExists();
            TryRecreateTestBlob(containerClient);
            logger.CreateBlobClientStop(blobSettings.Account, blobSettings.AuthMode.ToString());

            var blobServiceClient = new BlobServiceClient(storageConnection);
            Func<string, BlobContainerClient> createContainerClient = name => new BlobContainerClient(storageConnection, name);

            return (blobServiceClient, containerClient, createContainerClient);
        }
        catch (Exception ex)
        {
            logger.CreateBlobClientFailed(blobSettings.Account, blobSettings.AuthMode.ToString(), ex.Message);
            throw;
        }
    }

    private void TryRecreateTestBlob(BlobContainerClient containerClient)
    {
        var isContainerExists = containerClient.Exists();
        if (!isContainerExists.Value)
        {
            throw new InvalidOperationException("Blob container is either not created or authn/authz failed");
        }

        var testBlob = containerClient.GetBlobClient("__test");
        testBlob.DeleteIfExists();
        var testData = JsonConvert.SerializeObject(new { name = "test" });
        testBlob.Upload(new MemoryStream(Encoding.UTF8.GetBytes(testData)));
        if (!testBlob.Exists())
        {
            throw new InvalidOperationException("Unable to create blob");
        }

        testBlob.Delete();
    }
}