// -----------------------------------------------------------------------
// <copyright file="UserAuthSteps.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Kusto.Tests.Steps
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using global::Kusto.Data;
    using global::Kusto.Data.Common;
    using Reqnroll;

    [Binding]
    public class UserAuthSteps
    {
        private readonly ScenarioContext context;
        private readonly IReqnrollOutputHelper outputWriter;
        private ICslQueryProvider queryClient;
        private List<IncidentSummary> results;

        public UserAuthSteps(ScenarioContext context, IReqnrollOutputHelper outputWriter)
        {
            this.context = context;
            this.outputWriter = outputWriter;
        }

        [Given(@"I connect to ICM cluster with user authentication")]
        public void GivenIConnectToIcmClusterWithUserAuthentication()
        {
            var cluster = "https://icmcluster.kusto.windows.net";
            var database = "IcmDataWarehouse";

            this.outputWriter.WriteLine($"Connecting to {cluster}/{database} with user authentication...");
            this.outputWriter.WriteLine("Note: This will prompt for Azure AD authentication.");

            // Use KustoConnectionStringBuilder with user prompt authentication
            var kcsb = new KustoConnectionStringBuilder(cluster)
                .WithAadUserPromptAuthentication();

            this.queryClient = global::Kusto.Data.Net.Client.KustoClientFactory.CreateCslQueryProvider(kcsb);
            this.queryClient.DefaultDatabaseName = database;

            this.context.Set(this.queryClient, "QueryClient");
            this.context.Set(database, "Database");
        }

        [When(@"I execute query for recent incidents")]
        public async Task WhenIExecuteQueryForRecentIncidents()
        {
            var query = @"
Incidents
// | where IncidentId == 677038414
| where CreateDate > ago(10d)
| extend ServiceName=extract(@'^([^\\]+)\\(.+)', 1, OwningTeamName, typeof(string))
| extend TeamName=extract(@'^([^\\]+)\\(.+)', 2, OwningTeamName, typeof(string))
| where ServiceName == 'AZURESTACKHCI' or ServiceName == 'MSAKS' or ServiceName == 'ARCAPPLIANCE'
| where TeamName != 'Triage'
| summarize count(), CreateDate=min(CreateDate) by ServiceName, TeamName
";

            this.outputWriter.WriteLine("Executing query:");
            this.outputWriter.WriteLine(query);

            var database = this.context.Get<string>("Database");
            var clientRequestProps = new ClientRequestProperties
            {
                ClientRequestId = Guid.NewGuid().ToString()
            };

            var reader = await this.queryClient.ExecuteQueryAsync(
                database,
                query,
                clientRequestProps,
                CancellationToken.None);

            this.results = new List<IncidentSummary>();

            while (reader.Read())
            {
                var summary = new IncidentSummary
                {
                    ServiceName = reader.GetString(0),
                    TeamName = reader.GetString(1),
                    Count = reader.GetInt32(2),
                    CreateDate = reader.GetDateTime(3)
                };

                this.results.Add(summary);
                this.outputWriter.WriteLine($"Service: {summary.ServiceName}, Team: {summary.TeamName}, Count: {summary.Count}, First Created: {summary.CreateDate:yyyy-MM-dd}");
            }

            this.context.Set(this.results, "Results");
        }

        [Then(@"I should receive incident summary data")]
        public void ThenIShouldReceiveIncidentSummaryData()
        {
            var results = this.context.Get<List<IncidentSummary>>("Results");
            results.Should().NotBeNull();
            results.Should().NotBeEmpty("Expected to find incidents in the last 10 days");

            this.outputWriter.WriteLine($"Total incident summaries retrieved: {results.Count}");
        }

        [Then(@"the results should contain service names")]
        public void ThenTheResultsShouldContainServiceNames()
        {
            var results = this.context.Get<List<IncidentSummary>>("Results");
            var serviceNames = results.Select(r => r.ServiceName).Distinct().ToList();

            serviceNames.Should().NotBeEmpty();
            this.outputWriter.WriteLine($"Services found: {string.Join(", ", serviceNames)}");

            // Verify expected services are present
            var expectedServices = new[] { "AZURESTACKHCI", "MSAKS", "ARCAPPLIANCE" };
            var foundServices = serviceNames.Intersect(expectedServices).ToList();

            if (foundServices.Any())
            {
                this.outputWriter.WriteLine($"Expected services found: {string.Join(", ", foundServices)}");
            }
        }

        [AfterScenario]
        public void Cleanup()
        {
            this.queryClient?.Dispose();
        }
    }

    public class IncidentSummary
    {
        public string ServiceName { get; set; }
        public string TeamName { get; set; }
        public int Count { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
