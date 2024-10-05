// -----------------------------------------------------------------------
// <copyright file="BatchIngestSteps.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Kusto.Tests.Steps
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Newtonsoft.Json;
    using TechTalk.SpecFlow;
    using TechTalk.SpecFlow.Infrastructure;

    [Binding]
    public class BatchIngestSteps
    {
        private readonly ScenarioContext context;
        private readonly FeatureContext featureContext;
        private readonly ISpecFlowOutputHelper outputWriter;

        public BatchIngestSteps(ScenarioContext context, FeatureContext featureContext, ISpecFlowOutputHelper outputWriter)
        {
            this.context = context;
            this.featureContext = featureContext;
            this.outputWriter = outputWriter;
        }

        [Given(@"json file folder ""(.*)""")]
        public void GivenJsonFileFolder(string inputFolder)
        {
            this.context.Set(inputFolder, "InputJsonFolder");
        }

        [When(@"Ensure kusto table ""([^""]*)"" exists")]
        public async Task WhenEnsureKustoTableExists(string tableName)
        {
            this.context.Set(tableName, "TableName");
            var kustoClient = this.featureContext.Get<IKustoClient>();
            await kustoClient.EnsureTable<Person>(tableName);
        }

        [When(@"I ingest json files")]
        public async Task WhenIIngestJsonFiles()
        {
            var inputJsonFolder = this.context.Get<string>("InputJsonFolder");
            var jsonFiles = Directory.GetFiles(inputJsonFolder, "*.json", SearchOption.TopDirectoryOnly);
            var people = new List<Person>();
            var kustoClient = this.featureContext.Get<IKustoClient>();
            var tableName = this.context.Get<string>("TableName");
            var totalAdded = 0;

            foreach (var jsonFile in jsonFiles)
            {
                var json = await File.ReadAllTextAsync(jsonFile);
                var group = JsonConvert.DeserializeObject<List<Person>>(json);
                if (group?.Count > 0)
                {
                    people.AddRange(group);
                    var added = await kustoClient.BulkInsertFromFile<Person>(jsonFile, tableName, default);
                    totalAdded += added;
                }
            }
            this.outputWriter.WriteLine($"Ingesting {people.Count} records");
            totalAdded.Should().Be(people.Count);
        }

        [Then(@"kusto db should have table ""[^""]*""")]
        public async Task ThenKustoDbShouldHaveTableWithData(string tableName)
        {
            var kustoClient = this.featureContext.Get<IKustoClient>();
            var query = $".show tables | where TableName == '{tableName}'";
            var tables = await kustoClient.ExecuteQuery<string>(query);
            var tableNames = tables.ToList();
            tableNames.Should().NotBeNullOrEmpty();
            tableNames.Should().HaveCount(1);
            tableNames.First().Should().Be(tableName);
        }

        [Then(@"table ""([^""]*)"" should have (.+) rows")]
        public async Task ThenTableShouldHaveRows(string tableName, int count)
        {
            var kustoClient = this.featureContext.Get<IKustoClient>();
            var actualCount = await kustoClient.GetTableRecordCount(tableName, default);
            actualCount.Should().Be(count);
        }
    }
}