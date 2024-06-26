// -----------------------------------------------------------------------
// <copyright file="AadTokenProvider.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Auth;

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Config;
using KeyVault;
using Logger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Settings;

public class AadTokenProvider
{
    private readonly AadSettings aadSettings;
    private readonly ILogger logger;
    private readonly IServiceProvider serviceProvider;

    public AadTokenProvider(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        aadSettings = configuration.GetConfiguredSettings<AadSettings>();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        logger = loggerFactory.CreateLogger<AadTokenProvider>();
    }

    public async Task<string> GetAccessTokenAsync(Guid correlationId, CancellationToken cancellationToken, string[]? scopes = null)
    {
        if (string.IsNullOrEmpty(aadSettings.ClientSecretName))
        {
            throw new InvalidOperationException("client secret must be specified for aad settings");
        }

        logger.GettingAccessToken(aadSettings.Scenarios, aadSettings.ClientSecretSource);

        X509Certificate2? clientCert = null;
        var scopesToUse = scopes ?? aadSettings.Scopes;
        if (scopesToUse?.Any() != true)
        {
            scopesToUse = new[] { "https://graph.microsoft.com/.default" };
        }

        string? accessToken;
        DateTimeOffset? expiresOn;

        try
        {
            switch (this.aadSettings.Scenarios)
            {
                case AadAuthScenarios.ConfidentialApp:
                    IConfidentialClientApplication app;
                    (app, clientCert) = this.CreateConfidentialApp(cancellationToken);
                    AuthenticationResult authResult = await app.AcquireTokenForClient(scopesToUse).ExecuteAsync(cancellationToken);
                    accessToken = authResult.AccessToken;
                    expiresOn = authResult.ExpiresOn;
                    break;
                case AadAuthScenarios.PublicApp:
                    IPublicClientApplication pubApp = this.CreatePublicClientApplication();
                    AuthenticationResult authResult2 = await pubApp.AcquireTokenInteractive(scopesToUse).ExecuteAsync(cancellationToken);
                    accessToken = authResult2.AccessToken;
                    expiresOn = authResult2.ExpiresOn;
                    break;
                case AadAuthScenarios.InteractiveUser:
                    var browserCredential = new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
                    {
                        TenantId = this.aadSettings.TenantId,
                    });
                    AccessToken tokenResult2 = await browserCredential.GetTokenAsync(new TokenRequestContext(scopesToUse), cancellationToken);
                    accessToken = tokenResult2.Token;
                    expiresOn = tokenResult2.ExpiresOn;
                    break;
                case AadAuthScenarios.ManagedIdentity:
                    var credential = new DefaultAzureCredential();
                    AccessToken tokenResult = await credential.GetTokenAsync(new TokenRequestContext(scopesToUse), cancellationToken);
                    accessToken = tokenResult.Token;
                    expiresOn = tokenResult.ExpiresOn;
                    break;
                default:
                    throw new NotSupportedException($"Usage scenario {this.aadSettings.Scenarios} is not supported");
            }
        }
        catch (Exception ex)
        {
            logger.FailedToGetAccessToken(ex);
            throw;
        }
        finally
        {
            clientCert?.Dispose();
        }

        if (accessToken == null)
        {
            var error = new InvalidOperationException("Failed to obtain the access token");
            logger.FailedToGetAccessToken(error);
            throw error;
        }

        logger.GotAccessToken(scopesToUse, expiresOn.Value);
        return accessToken;
    }

    /// <summary>
    /// Gets the client credential.
    /// </summary>
    /// <param name="cancellationToken">The cancel token</param>
    /// <returns>Tuple of TokenCredential and X509Certificate2</returns>
    public (TokenCredential tokenCredential, X509Certificate2? clientCert) GetClientCredential(CancellationToken cancellationToken)
    {
        (string? clientSecret, X509Certificate2? clientCert) = this.GetClientSecretOrCert(this.aadSettings, cancellationToken);
        if (clientSecret != null)
        {
            return (new ClientSecretCredential(this.aadSettings.TenantId, this.aadSettings.ClientId, clientSecret), null);
        }

        return (new ClientCertificateCredential(this.aadSettings.TenantId, this.aadSettings.ClientId, clientCert), clientCert);
    }

    /// <summary>
    /// Gets the client secret or certificate.
    /// </summary>
    /// <param name="newAadSettings">AAD settings</param>
    /// <param name="cancellationToken">The cancel token</param>
    /// <returns>Client secret and cert</returns>
    /// <exception cref="InvalidOperationException">InvalidConfigurationException</exception>
    /// <exception cref="NotSupportedException">NotSupportedException</exception>
    public (string? clientSecret, X509Certificate2? clientCert) GetClientSecretOrCert(AadSettings newAadSettings, CancellationToken cancellationToken)
    {
        ISecretProvider? secretProvider = this.serviceProvider.GetService<ISecretProvider>();

        switch (newAadSettings.ClientSecretSource)
        {
            case AadClientSecretSource.ClientSecretFromFile:
                var clientSecretFilePath = GetSecretOrCertFile(newAadSettings.ClientSecretName);
                var clientSecretFromFile = File.ReadAllText(clientSecretFilePath);
                return (clientSecretFromFile, null);
            case AadClientSecretSource.ClientCertFromFile:
                var clientCertFilePath = GetSecretOrCertFile(newAadSettings.ClientSecretName);
                var clientCertFromFile = new X509Certificate2(clientCertFilePath);
                return (null, clientCertFromFile);
            case AadClientSecretSource.ClientSecretFromVault:
                var clientSecretName = newAadSettings.ClientSecretName;
                if (secretProvider == null)
                {
                    throw new InvalidOperationException($"Aad client secret source is {newAadSettings.ClientSecretSource} but secret provider is not available");
                }

                var clientSecretFromVault = GetSecret(secretProvider, clientSecretName);
                return (clientSecretFromVault, null);
            case AadClientSecretSource.ClientCertFromVault:
                var clientCertName = newAadSettings.ClientSecretName;
                if (secretProvider == null)
                {
                    throw new InvalidOperationException($"Aad client secret source is {newAadSettings.ClientSecretSource} but secret provider is not available");
                }

                X509Certificate2 clientCertFromVault = GetCert(secretProvider, clientCertName);
                return (null, clientCertFromVault);
            default:
                throw new NotSupportedException($"client secret source {newAadSettings.ClientSecretSource} is not supported");
        }
    }

    public static OSPlatform GetOSPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return OSPlatform.Windows;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return OSPlatform.Linux;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return OSPlatform.OSX;
        }

        throw new PlatformNotSupportedException("The current operating system is not supported.");
    }

    internal static string GetSecretOrCertFile(string secretOrCertFile)
    {
        var secretOrCertFilePath = secretOrCertFile;
        if (!File.Exists(secretOrCertFilePath))
        {
            var osPlatform = GetOSPlatform();
            if (osPlatform == OSPlatform.Windows)
            {
                var homeFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                secretOrCertFilePath = Path.Combine(homeFolder, ".secrets", secretOrCertFile);
            }
            else
            {
                secretOrCertFilePath = Path.Combine("/tmp/.secrets", secretOrCertFile);
            }
        }

        if (!File.Exists(secretOrCertFilePath))
        {
            throw new IOException($"unable to find client secret/cert file: {secretOrCertFilePath}");
        }

        return secretOrCertFilePath;
    }

    private static string GetSecret(ISecretProvider secretClient, string secretName)
    {
        var result = secretClient.GetSecret(secretName);
        if (result == null)
        {
            throw new InvalidOperationException($"unable to find secret: {secretName}");
        }

        return result;
    }

    private static X509Certificate2 GetCert(ISecretProvider secretProvider, string certName)
    {
        var result = secretProvider.GetCert(certName);
        if (result == null)
        {
            throw new InvalidOperationException($"unable to find certificate: {certName}");
        }

        return result;
    }

    private (IConfidentialClientApplication app, X509Certificate2? clientCert) CreateConfidentialApp(CancellationToken cancellationToken)
    {
        var (clientSecret, clientCert) = this.GetClientSecretOrCert(this.aadSettings, cancellationToken);
        ConfidentialClientApplicationBuilder appBuilder;
        if (clientSecret != null)
        {
            appBuilder = ConfidentialClientApplicationBuilder.Create(this.aadSettings.ClientId)
                .WithClientSecret(clientSecret)
                .WithAuthority(new Uri(this.aadSettings.Authority));
        }
        else if (clientCert != null)
        {
            appBuilder = ConfidentialClientApplicationBuilder.Create(this.aadSettings.ClientId)
                .WithCertificate(clientCert)
                .WithAuthority(new Uri(this.aadSettings.Authority));
        }
        else
        {
            throw new InvalidOperationException("client secret or certificate must be specified for aad settings");
        }

        IConfidentialClientApplication app = appBuilder.Build();

        return (app, clientCert);
    }

    private IPublicClientApplication CreatePublicClientApplication()
    {
        PublicClientApplicationBuilder pubAppBuilder = PublicClientApplicationBuilder
            .Create(this.aadSettings.ClientId)
            .WithAuthority(this.aadSettings.Authority);
        pubAppBuilder = this.aadSettings.RedirectUrl != null
            ? pubAppBuilder.WithRedirectUri(this.aadSettings.RedirectUrl.ToString())
            : pubAppBuilder.WithDefaultRedirectUri();

        IPublicClientApplication pubApp = pubAppBuilder.Build();
        return pubApp;
    }
}