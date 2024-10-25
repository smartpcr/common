// -----------------------------------------------------------------------
// <copyright file="KeyVaultBuilder.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.KeyVault;

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using Config;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

public static class KeyVaultBuilder
{
    public static void AddKeyVault(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceProvider = services.BuildServiceProvider();
        var vaultSettingsOptions = serviceProvider.GetService<IOptions<VaultSettings>>();
        var vaultSettings = vaultSettingsOptions?.Value ?? configuration.GetConfiguredSettings<VaultSettings>();
        Console.WriteLine($"registering KeyVault with auth type {vaultSettings.AuthType}");

        if (vaultSettings.AuthType == VaultAuthType.Msi || vaultSettings.AuthType == VaultAuthType.User)
        {
            services.AddSingleton<SecretClient>(_ =>
            {
                var tokenCredential = CreateTokenCredential(vaultSettings);
                Console.WriteLine("registering secret client");
                return new SecretClient(vaultSettings.VaultUrl, tokenCredential);
            });
            services.AddSingleton<CertificateClient>(_ =>
            {
                var tokenCredential = CreateTokenCredential(vaultSettings);
                Console.WriteLine("registering certificate client");
                return new CertificateClient(vaultSettings.VaultUrl, tokenCredential);
            });
        }
        else
        {
            var aadSettingsOptions = serviceProvider.GetService<IOptions<AadSettings>>();
            var aadSettings = aadSettingsOptions?.Value ?? configuration.GetConfiguredSettings<AadSettings>();
            var vaultAadSettings = vaultSettings.Aad ?? new VaultAadSettings
            {
                TenantId = aadSettings.TenantId,
                ClientId = aadSettings.ClientId,
                SecretFileName = aadSettings.ClientSecretName
            };
            var clientSecretFilePath = GetSecretOrCertFile(vaultAadSettings.SecretFileName);

            services.AddSingleton<KeyVaultClient>(_ =>
            {
                Console.WriteLine("registering keyvault client");
                string? clientSecretFromFile = null;
                X509Certificate2? clientCertFromFile = null;
                if (vaultSettings.AuthType == VaultAuthType.SpnWithCertOnFile)
                {
                    clientCertFromFile = new X509Certificate2(clientSecretFilePath);
                }
                else if (vaultSettings.AuthType == VaultAuthType.SpnWithSecretOnFile)
                {
                    clientSecretFromFile = File.ReadAllText(clientSecretFilePath).Trim();
                }
                else
                {
                    throw new NotSupportedException($"Vault auth type {vaultSettings.AuthType} is not supported");
                }

                var authCallback = AuthenticationCallbackUsingClient(vaultAadSettings.ClientId, clientSecretFromFile, clientCertFromFile);
                return new KeyVaultClient(authCallback);
            });
        }

        services.AddSingleton<ISecretProvider, SecretProvider>();

    }

    private static TokenCredential CreateTokenCredential(VaultSettings vaultSettings)
    {
        return vaultSettings.AuthType switch
        {
            VaultAuthType.Msi => new DefaultAzureCredential(),
            VaultAuthType.User => CreateDevCredInternal(),
            _ => throw new NotSupportedException($"Vault auth type {vaultSettings.AuthType} is not supported"),
        };
    }

    private static KeyVaultClient.AuthenticationCallback AuthenticationCallbackUsingClient(
        string clientAppId,
        string? clientSecret,
        X509Certificate2? clientCert) =>
        async (authority, resource, _) =>
        {
            var clientBuilder = ConfidentialClientApplicationBuilder.Create(clientAppId).WithAuthority(authority);
            clientBuilder = clientSecret != null
                ? clientBuilder.WithClientSecret(clientSecret)
                : clientBuilder.WithCertificate(clientCert, sendX5C: true);

            IConfidentialClientApplication confidentialClientApplication = clientBuilder
                .WithLegacyCacheCompatibility(false)
                .Build();

            var result = await confidentialClientApplication.AcquireTokenForClient(
                scopes: new[] { $"{resource}/.default" })
                .ExecuteAsync()
                .ConfigureAwait(false);

            if (result == null)
            {
                throw new InvalidOperationException(
                    "Failed to retrieve access token for KeyVault client");
            }

            return result.AccessToken;
        };

    internal static string GetSecretOrCertFile(string secretOrCertFile)
    {
        var secretOrCertFilePath = secretOrCertFile;
        if (!File.Exists(secretOrCertFilePath))
        {
            var homeFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            secretOrCertFilePath = Path.Combine(homeFolder, ".secrets", secretOrCertFile);

            if (!File.Exists(secretOrCertFilePath))
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

    internal static TokenCredential CreateDevCredInternal()
    {
        string? token = Environment.GetEnvironmentVariable("DEV_KEYVAULT_ACCESS_TOKEN");
        if (!string.IsNullOrWhiteSpace(token))
            return new StaticTokenCredential(token, TimeProvider.System);
        return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"))
            ? new DeviceCodeTokenCredential("https://vault.azure.net")
            : new DefaultAzureCredential();
    }
}