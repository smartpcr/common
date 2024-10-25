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

public class KustoAuthHelper
{
    private readonly IServiceProvider serviceProvider;
    private readonly IConfiguration configuration;
    private readonly KustoSettings kustoSettings;
    private readonly KustoConnectionStringBuilder kustoConnectionStringBuilder;

    public KustoAuthHelper(IServiceProvider serviceProvider, KustoSettings? kustoSettings = null)
    {
        this.serviceProvider = serviceProvider;
        this.configuration = serviceProvider.GetRequiredService<IConfiguration>();
        this.kustoSettings = kustoSettings ?? this.configuration.GetConfiguredSettings<KustoSettings>();
        this.kustoConnectionStringBuilder = this.GetConnStringBuilder();
    }

    public ICslQueryProvider QueryQueryClient
    {
        get
        {
            var queryClient = global::Kusto.Data.Net.Client.KustoClientFactory.CreateCslQueryProvider(this.kustoConnectionStringBuilder);
            queryClient.DefaultDatabaseName = this.kustoSettings.DbName;
            return queryClient;
        }
    }

    public ICslAdminProvider AdminClient
    {
        get
        {
            var adminClient = global::Kusto.Data.Net.Client.KustoClientFactory.CreateCslAdminProvider(this.kustoConnectionStringBuilder);
            adminClient.DefaultDatabaseName = this.kustoSettings.DbName;
            return adminClient;
        }
    }

    public IKustoIngestClient DirectIngestClient => KustoIngestFactory.CreateDirectIngestClient(this.kustoConnectionStringBuilder);

    public IKustoIngestClient StreamingIngestClient => KustoIngestFactory.CreateStreamingIngestClient(this.kustoConnectionStringBuilder);

    public IKustoQueuedIngestClient QueuedIngestClient => KustoIngestFactory.CreateQueuedIngestClient(this.kustoConnectionStringBuilder);

    private KustoConnectionStringBuilder GetConnStringBuilder()
    {
        if (this.kustoSettings.AuthMode == KustoAuthMode.None)
        {
            return new KustoConnectionStringBuilder($"{this.kustoSettings.ClusterUrl}") { InitialCatalog = this.kustoSettings.DbName };
        }

        var aadSettings = this.kustoSettings.Aad ?? this.configuration.GetConfiguredSettings<AadSettings>();
        var tokenProvider = new AadTokenProvider(this.serviceProvider);

        switch (this.kustoSettings.AuthMode)
        {
            case KustoAuthMode.Msi:
                return new KustoConnectionStringBuilder($"{this.kustoSettings.ClusterUrl}")
                {
                    InitialCatalog = this.kustoSettings.DbName,
                    FederatedSecurity = true,
                    EmbeddedManagedIdentity = "system"
                };
            case KustoAuthMode.Spn:
                var (clientSecret, clientCert) = tokenProvider.GetClientSecretOrCert(aadSettings, CancellationToken.None);
                if (clientSecret != null)
                {
                    return new KustoConnectionStringBuilder($"{this.kustoSettings.ClusterUrl}") { InitialCatalog = this.kustoSettings.DbName }
                        .WithAadApplicationKeyAuthentication(
                            aadSettings.ClientId,
                            clientSecret,
                            aadSettings.Authority);
                }

                return new KustoConnectionStringBuilder($"{this.kustoSettings.ClusterUrl}") { InitialCatalog = this.kustoSettings.DbName }
                    .WithAadApplicationCertificateAuthentication(
                        aadSettings.ClientId,
                        clientCert,
                        aadSettings.Authority);
            case KustoAuthMode.User:
                aadSettings.Scenarios = AadAuthScenarios.InteractiveUser;
                return new KustoConnectionStringBuilder($"{this.kustoSettings.ClusterUrl}") { InitialCatalog = this.kustoSettings.DbName }
                    .WithAadUserPromptAuthentication(
                        aadSettings.ClientId,
                        aadSettings.Authority);
            default:
                throw new NotSupportedException($"Kusto auth mode {this.kustoSettings.AuthMode} is not supported");
        }
    }
}