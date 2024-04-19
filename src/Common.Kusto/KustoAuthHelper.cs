// -----------------------------------------------------------------------
// <copyright file="KustoAuthHelper.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Kusto;

using System;
using System.Threading;
using Auth;
using Config;
using global::Kusto.Data;
using global::Kusto.Data.Common;
using global::Kusto.Ingest;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Settings;

public class KustoAuthHelper
{
    private readonly IServiceProvider serviceProvider;
    private readonly IConfiguration configuration;
    private readonly KustoSettings kustoSettings;
    private readonly KustoConnectionStringBuilder kustoConnectionStringBuilder;

    public KustoAuthHelper(IServiceProvider serviceProvider, KustoSettings? kustoSettings = null)
    {
        this.serviceProvider = serviceProvider;
        configuration = serviceProvider.GetRequiredService<IConfiguration>();
        this.kustoSettings = kustoSettings ?? configuration.GetConfiguredSettings<KustoSettings>();
        kustoConnectionStringBuilder = GetConnStringBuilder();
    }

    public ICslQueryProvider QueryQueryClient
    {
        get
        {
            var queryClient = global::Kusto.Data.Net.Client.KustoClientFactory.CreateCslQueryProvider(kustoConnectionStringBuilder);
            queryClient.DefaultDatabaseName = kustoSettings.DbName;
            return queryClient;
        }
    }

    public ICslAdminProvider AdminClient
    {
        get
        {
            var adminClient = global::Kusto.Data.Net.Client.KustoClientFactory.CreateCslAdminProvider(kustoConnectionStringBuilder);
            adminClient.DefaultDatabaseName = kustoSettings.DbName;
            return adminClient;
        }
    }

    public IKustoIngestClient IngestClient => KustoIngestFactory.CreateDirectIngestClient(kustoConnectionStringBuilder);

    private KustoConnectionStringBuilder GetConnStringBuilder()
    {
        var aadSettings = kustoSettings.Aad ?? configuration.GetConfiguredSettings<AadSettings>();
        var tokenProvider = new AadTokenProvider(serviceProvider);

        switch (kustoSettings.AuthMode)
        {
            case KustoAuthMode.Msi:
                return new KustoConnectionStringBuilder($"{kustoSettings.ClusterUrl}")
                {
                    InitialCatalog = kustoSettings.DbName,
                    FederatedSecurity = true,
                    EmbeddedManagedIdentity = "system"
                };
            case KustoAuthMode.Spn:
                var (clientSecret, clientCert) = tokenProvider.GetClientSecretOrCert(aadSettings, CancellationToken.None);
                if (clientSecret != null)
                {
                    return new KustoConnectionStringBuilder($"{kustoSettings.ClusterUrl}") { InitialCatalog = kustoSettings.DbName }
                        .WithAadApplicationKeyAuthentication(
                            aadSettings.ClientId,
                            clientSecret,
                            aadSettings.Authority);
                }

                return new KustoConnectionStringBuilder($"{kustoSettings.ClusterUrl}") { InitialCatalog = kustoSettings.DbName }
                    .WithAadApplicationCertificateAuthentication(
                        aadSettings.ClientId,
                        clientCert,
                        aadSettings.Authority);
            case KustoAuthMode.User:
                aadSettings.Scenarios = AadAuthScenarios.InteractiveUser;
                return new KustoConnectionStringBuilder($"{kustoSettings.ClusterUrl}") { InitialCatalog = kustoSettings.DbName }
                    .WithAadUserPromptAuthentication(
                        aadSettings.ClientId,
                        aadSettings.Authority);
            default:
                throw new NotSupportedException($"Kusto auth mode {kustoSettings.AuthMode} is not supported");
        }
    }
}