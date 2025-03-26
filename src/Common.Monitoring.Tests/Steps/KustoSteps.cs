// -----------------------------------------------------------------------
// <copyright file="KustoSteps.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Tests.Steps
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using FluentAssertions;
    using global::Kusto.Data;
    using global::Kusto.Data.Common;
    using global::Kusto.Data.Net.Client;
    using Reqnroll;
    using Reqnroll.Infrastructure;

    [Binding]
    public class KustoSteps
    {
        private readonly ScenarioContext context;
        private readonly IReqnrollOutputHelper outputWriter;

        public KustoSteps(ScenarioContext context, IReqnrollOutputHelper outputWriter)
        {
            this.context = context;
            this.outputWriter = outputWriter;
        }

        [Given("kusto cluster uri \"([^\"]+)\"")]
        public async Task GivenKustoClusterUri(string kustoClusterUri)
        {
            var httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(1)
            };
            var response = await httpClient.GetAsync(kustoClusterUri);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            this.context.Set(kustoClusterUri, "kustoClusterUri");
            var connectionStringBuilder = new KustoConnectionStringBuilder($"{kustoClusterUri}")
            {
                InitialCatalog = "NetDefaultDB"
            };
            var adminClient = KustoClientFactory.CreateCslAdminProvider(connectionStringBuilder);
            this.context.Set(adminClient, "adminClient");
            var queryClient = KustoClientFactory.CreateCslQueryProvider(connectionStringBuilder);
            this.context.Set(queryClient, "queryClient");
        }

        [Given("kusto database name \"([^\"]+)\"")]
        public void GivenKustoDatabaseName(string dbName)
        {
            var adminClient = this.context.Get<ICslAdminProvider>("adminClient");
            var showDatabasesCommand = ".show databases";
            using var result = adminClient.ExecuteControlCommand(showDatabasesCommand);
            var dbExists = false;
            while (result.Read())
            {
                if (result.GetString(0) == dbName)
                {
                    dbExists = true;
                    break;
                }
            }

            if (!dbExists)
            {
                var createDatabaseCommand = @$".create database {dbName} persist (
      @""/kustodata/dbs/{dbName}/md"",
      @""/kustodata/dbs/{dbName}/data""
    )";
                adminClient.ExecuteControlCommand(createDatabaseCommand);
                this.outputWriter.WriteLine($"Database {dbName} created");
            }

            var kustoClusterUri = this.context.Get<string>("kustoClusterUri");
            var connectionStringBuilder = new KustoConnectionStringBuilder($"{kustoClusterUri}")
            {
                InitialCatalog = dbName
            };
            adminClient = KustoClientFactory.CreateCslAdminProvider(connectionStringBuilder);
            this.context.Set(adminClient, "adminClient");
            var queryClient = KustoClientFactory.CreateCslQueryProvider(connectionStringBuilder);
            this.context.Set(queryClient, "queryClient");

            this.context.Set(dbName, "dbName");
        }

        [Given("kustainer volume mount from \"([^\"]+)\" to \"([^\"]+)\"")]
        public void GivenKustainerVolumeMountFromTo(string hostPath, string containerPath)
        {
            this.context.Set(hostPath, "hostPath");
            this.context.Set(containerPath, "containerPath");
        }

    }
}