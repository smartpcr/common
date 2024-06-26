// -----------------------------------------------------------------------
// <copyright file="SecretProvider.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.KeyVault;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using Config;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using Settings;

public class SecretProvider : ISecretProvider
{
    private readonly ILogger<SecretProvider> logger;
    private readonly Tracer tracer;
    private readonly VaultSettings vaultSettings;
    private readonly SecretClient? secretClient;
    private readonly CertificateClient? certificateClient;
    private readonly KeyVaultClient? keyVaultClient;
    private readonly SecretProviderMeter meter;

    public SecretProvider(
        IServiceProvider serviceProvider,
        ILogger<SecretProvider> logger)
    {
        this.logger = logger;
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var metadata = configuration.GetConfiguredSettings<ApplicationMetadata>();
        var traceProvider = serviceProvider.GetRequiredService<TracerProvider>();
        tracer = traceProvider.GetTracer(metadata.ApplicationName + $".{nameof(SecretProvider)}", metadata.BuildVersion);
        this.meter = SecretProviderMeter.Instance(metadata);

        var vaultSettingsOptions = serviceProvider.GetService<IOptions<VaultSettings>>();
        vaultSettings = vaultSettingsOptions?.Value ?? configuration.GetConfiguredSettings<VaultSettings>();
        var aadSettingsOptions = serviceProvider.GetService<IOptions<AadSettings>>();
        var aadSettings = aadSettingsOptions?.Value ?? configuration.GetConfiguredSettings<AadSettings>();
        vaultSettings.Aad ??= new VaultAadSettings
        {
            TenantId = aadSettings.TenantId,
            ClientId = aadSettings.ClientId,
            SecretFileName = aadSettings.ClientSecretName
        };

        if (vaultSettings.AuthType == VaultAuthType.Msi || vaultSettings.AuthType == VaultAuthType.User)
        {
            secretClient = serviceProvider.GetRequiredService<SecretClient>();
            certificateClient = serviceProvider.GetRequiredService<CertificateClient>();
            keyVaultClient = null;
        }
        else
        {
            keyVaultClient = serviceProvider.GetRequiredService<KeyVaultClient>();
            secretClient = null;
            certificateClient = null;
        }
    }

    public async Task<IList<string>> ListSecretsAsync(CancellationToken cancel)
    {
        using var span = tracer.StartActiveSpan(nameof(ListSecretsAsync));
        logger.ListSecretNamesStart(vaultSettings.VaultName, vaultSettings.AuthType.ToString());
        var watch = Stopwatch.StartNew();
        try
        {
            var secretNames = new List<string>();
            if (secretClient != null)
            {
                await foreach (var secretProperties in secretClient.GetPropertiesOfSecretsAsync(cancel))
                {
                    secretNames.Add(secretProperties.Name);
                }
            }
            else
            {
                // max results of 25 is a hard limit
                var secrets = await keyVaultClient.GetSecretsAsync(vaultSettings.VaultUrl.ToString(), 25, cancel);
                while (secrets?.Any() == true)
                {
                    secretNames.AddRange(secrets.Select(s => s.Identifier.Name));
                    if (secrets.NextPageLink != null)
                    {
                        secrets = await keyVaultClient.GetSecretsNextAsync(secrets.NextPageLink, cancel);
                    }
                    else
                    {
                        secrets = null;
                    }
                }
            }

            logger.ListSecretNamesStop(vaultSettings.VaultName, vaultSettings.AuthType.ToString(), secretNames.Count, watch.ElapsedMilliseconds);
            return secretNames;
        }
        catch (Exception ex)
        {
            logger.ListSecretNamesFailed(vaultSettings.VaultName, vaultSettings.AuthType.ToString(), ex.Message, watch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<string> GetSecretAsync(string secretName, CancellationToken cancel)
    {
        using var span = tracer.StartActiveSpan(nameof(GetSecretAsync));
        logger.GetSecretStart(secretName, vaultSettings.VaultName, vaultSettings.AuthType.ToString());
        var watch = Stopwatch.StartNew();
        var metricDimensions = new[]
        {
            new KeyValuePair<string, object?>(nameof(this.vaultSettings.AuthType), this.vaultSettings.AuthType),
            new KeyValuePair<string, object?>(nameof(secretName), secretName)
        };

        try
        {
            string? secretValue;
            if (secretClient != null)
            {
                var secret = await secretClient.GetSecretAsync(secretName, null, cancel);
                secretValue = secret.Value.Value;
            }
            else
            {
                var secret = await keyVaultClient.GetSecretAsync(vaultSettings.VaultUrl.ToString(), secretName, cancel);
                secretValue = secret.Value;
            }

            logger.GetSecretSucceed(secretName, vaultSettings.VaultName, vaultSettings.AuthType.ToString(), watch.ElapsedMilliseconds);
            return secretValue;
        }
        catch (Exception ex)
        {
            this.meter.IncrementTotalSecretFailures(metricDimensions);
            logger.GetSecretFailed(secretName, vaultSettings.VaultName, vaultSettings.AuthType.ToString(), ex.Message, watch.ElapsedMilliseconds);
            throw;
        }
        finally
        {
            this.meter.RecordGetSecretDuration(watch.ElapsedMilliseconds, metricDimensions);
        }
    }

    public string GetSecret(string secretName)
    {
        using var span = tracer.StartActiveSpan(nameof(GetSecret));
        logger.GetSecretStart(secretName, vaultSettings.VaultName, vaultSettings.AuthType.ToString());
        var watch = Stopwatch.StartNew();
        var metricDimensions = new[]
        {
            new KeyValuePair<string, object?>(nameof(this.vaultSettings.AuthType), this.vaultSettings.AuthType),
            new KeyValuePair<string, object?>(nameof(secretName), secretName)
        };

        try
        {
            string? secretValue;
            if (secretClient != null)
            {
                var secret = secretClient.GetSecret(secretName);
                secretValue = secret.Value.Value;
            }
            else
            {
                var secret = keyVaultClient.GetSecretAsync(vaultSettings.VaultUrl.ToString(), secretName)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                secretValue = secret.Value;
            }

            logger.GetSecretSucceed(secretName, vaultSettings.VaultName, vaultSettings.AuthType.ToString(), watch.ElapsedMilliseconds);
            return secretValue;
        }
        catch (Exception ex)
        {
            this.meter.IncrementTotalSecretFailures(metricDimensions);
            logger.GetSecretFailed(secretName, vaultSettings.VaultName, vaultSettings.AuthType.ToString(), ex.Message, watch.ElapsedMilliseconds);
            throw;
        }
        finally
        {
            this.meter.RecordGetSecretDuration(watch.ElapsedMilliseconds, metricDimensions);
        }
    }

    public async Task<X509Certificate2> GetCertAsync(string certName, CancellationToken cancel)
    {
        using var span = tracer.StartActiveSpan(nameof(GetCertAsync));
        logger.GetCertStart(certName, vaultSettings.VaultName, vaultSettings.AuthType.ToString());
        var watch = Stopwatch.StartNew();
        var metricDimensions = new[]
        {
            new KeyValuePair<string, object?>(nameof(this.vaultSettings.AuthType), this.vaultSettings.AuthType),
            new KeyValuePair<string, object?>(nameof(certName), certName)
        };

        try
        {
            X509Certificate2? cert;
            if (certificateClient != null)
            {
                var certificate = await certificateClient.GetCertificateAsync(certName, cancel);
                cert = new X509Certificate2(certificate.Value.Cer);
            }
            else
            {
                var certificate = await keyVaultClient.GetCertificateAsync(vaultSettings.VaultUrl.ToString(), certName, cancel);
                cert = new X509Certificate2(certificate.Cer);
            }

            logger.GetCertSucceed(certName, vaultSettings.VaultName, vaultSettings.AuthType.ToString(), watch.ElapsedMilliseconds);
            return cert;
        }
        catch (Exception ex)
        {
            this.meter.IncrementTotalCertFailures(metricDimensions);
            logger.GetCertFailed(certName, vaultSettings.VaultName, vaultSettings.AuthType.ToString(), ex.Message, watch.ElapsedMilliseconds);
            throw;
        }
        finally
        {
            this.meter.RecordGetCertDuration(watch.ElapsedMilliseconds, metricDimensions);
        }
    }

    public X509Certificate2 GetCert(string certName)
    {
        using var span = tracer.StartActiveSpan(nameof(GetCert));
        logger.GetCertStart(certName, vaultSettings.VaultName, vaultSettings.AuthType.ToString());
        var watch = Stopwatch.StartNew();
        var metricDimensions = new[]
        {
            new KeyValuePair<string, object?>(nameof(this.vaultSettings.AuthType), this.vaultSettings.AuthType),
            new KeyValuePair<string, object?>(nameof(certName), certName)
        };

        try
        {
            X509Certificate2? cert;
            if (certificateClient != null)
            {
                var certificate = certificateClient.GetCertificate(certName);
                cert = new X509Certificate2(certificate.Value.Cer);
            }
            else
            {
                var certificate = keyVaultClient.GetCertificateAsync(vaultSettings.VaultUrl.ToString(), certName)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                cert = new X509Certificate2(certificate.Cer);
            }

            logger.GetCertSucceed(certName, vaultSettings.VaultName, vaultSettings.AuthType.ToString(), watch.ElapsedMilliseconds);
            return cert;
        }
        catch (Exception ex)
        {
            this.meter.IncrementTotalCertFailures(metricDimensions);
            logger.GetCertFailed(certName, vaultSettings.VaultName, vaultSettings.AuthType.ToString(), ex.Message, watch.ElapsedMilliseconds);
            throw;
        }
        finally
        {
            this.meter.RecordGetCertDuration(watch.ElapsedMilliseconds, metricDimensions);
        }
    }
}