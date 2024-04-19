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
using Microsoft.IdentityModel.Protocols.Configuration;
using Microsoft.R9.Extensions.Authentication.Msal;
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
            switch (aadSettings.Scenarios)
            {
                case AadAuthScenarios.ConfidentialApp:
                case AadAuthScenarios.PublicApp:
                    (var app, clientCert) = CreateConfidentialApp(cancellationToken);
                    var authResult = await app.AcquireTokenForClientAsync(
                        scopesToUse,
                        acquireTokenBuilder =>
                        {
                            acquireTokenBuilder
                                .WithCorrelationId(correlationId)
                                .WithTenantId(aadSettings.TenantId);
                        },
                        cancellationToken);
                    accessToken = authResult.AccessToken;
                    expiresOn = authResult.ExpiresOn;
                    break;
                case AadAuthScenarios.InteractiveUser:
                    var pubApp = CreatePublicClientApplication();
                    var authResult2 = await pubApp.AcquireTokenInteractive(scopesToUse).ExecuteAsync(cancellationToken);
                    accessToken = authResult2.AccessToken;
                    expiresOn = authResult2.ExpiresOn;
                    break;
                case AadAuthScenarios.ManagedIdentity:
                    var credential = new DefaultAzureCredential();
                    var tokenResult = await credential.GetTokenAsync(new TokenRequestContext(scopesToUse), cancellationToken);
                    accessToken = tokenResult.Token;
                    expiresOn = tokenResult.ExpiresOn;
                    break;
                default:
                    throw new NotSupportedException($"Usage scenario {aadSettings.Scenarios} is not supported");
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

        logger.GotAccessToken(scopesToUse, accessToken, expiresOn.Value);
        return accessToken;
    }

    public (TokenCredential tokenCredential, X509Certificate2? clientCert) GetClientCredential(CancellationToken cancellationToken)
    {
        var (clientSecret, clientCert) = GetClientSecretOrCert(aadSettings, cancellationToken);
        if (clientSecret != null)
        {
            return (new ClientSecretCredential(aadSettings.TenantId, aadSettings.ClientId, clientSecret), null);
        }

        return (new ClientCertificateCredential(aadSettings.TenantId, aadSettings.ClientId, clientCert), clientCert);
    }

    public (string? clientSecret, X509Certificate2? clientCert) GetClientSecretOrCert(AadSettings newAadSettings, CancellationToken cancellationToken)
    {
        var secretProvider = serviceProvider.GetService<ISecretProvider>();

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
                    throw new InvalidConfigurationException($"Aad client secret source is {newAadSettings.ClientSecretSource} but secret provider is not available");
                }

                var clientSecretFromVault = GetSecret(secretProvider, clientSecretName, cancellationToken);
                return (clientSecretFromVault, null);
            case AadClientSecretSource.ClientCertFromVault:
                var clientCertName = newAadSettings.ClientSecretName;
                if (secretProvider == null)
                {
                    throw new InvalidConfigurationException($"Aad client secret source is {newAadSettings.ClientSecretSource} but secret provider is not available");
                }

                var clientCertFromVault = GetCert(secretProvider, clientCertName, cancellationToken);
                return (null, clientCertFromVault);
            default:
                throw new NotSupportedException($"client secret source {newAadSettings.ClientSecretSource} is not supported");
        }
    }

    private static string GetSecret(ISecretProvider secretClient, string secretName, CancellationToken cancel)
    {
        var result = secretClient.GetSecret(secretName);
        if (result == null)
        {
            throw new InvalidOperationException($"unable to find secret: {secretName}");
        }

        return result;
    }

    private static X509Certificate2 GetCert(ISecretProvider secretProvider, string certName, CancellationToken cancel)
    {
        var result = secretProvider.GetCert(certName);
        if (result == null)
        {
            throw new InvalidOperationException($"unable to find certificate: {certName}");
        }

        return result;
    }

    private (IConfidentialClientApplicationAdapter app, X509Certificate2? clientCert) CreateConfidentialApp(CancellationToken cancellationToken)
    {
        (string? clientSecret, var clientCert) = GetClientSecretOrCert(aadSettings, cancellationToken);
        var appBuilder = ConfidentialClientApplicationBuilder
            .Create(aadSettings.ClientId);
        if (clientSecret != null)
        {
            appBuilder = appBuilder.WithClientSecret(clientSecret);
        }
        else if (clientCert != null)
        {
            appBuilder = appBuilder.WithCertificate(clientCert);
        }

        var app = appBuilder.BuildConfidentialClientApplicationAdapter(
            serviceProvider,
            _ => { },
            Microsoft.Extensions.Options.Options.Create(
                new MsalOptions
                {
                    EnableTokenCaching = true,
                    EnableCacheSynchronization = false,
                    EnableLegacyCacheCompatibility = false
                }));

        return (app, clientCert);
    }

    private IPublicClientApplication CreatePublicClientApplication()
    {
        var pubAppBuilder = PublicClientApplicationBuilder
            .Create(aadSettings.ClientId)
            .WithAuthority(aadSettings.Authority)
            .WithLogging(serviceProvider);
        pubAppBuilder = aadSettings.RedirectUrl != null
            ? pubAppBuilder.WithRedirectUri(aadSettings.RedirectUrl.ToString())
            : pubAppBuilder.WithDefaultRedirectUri();

        var pubApp = pubAppBuilder.Build();
        return pubApp;
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

    private static OSPlatform GetOSPlatform()
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
}