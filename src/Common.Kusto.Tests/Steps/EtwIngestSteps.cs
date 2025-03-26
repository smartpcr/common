// -----------------------------------------------------------------------
// <copyright file="EtwIngestSteps.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Kusto.Tests.Steps
{
    using System.Collections.Generic;
    using FluentAssertions;
    using Reqnroll;

    [Binding]
    public class EtwIngestSteps
    {
        private readonly ScenarioContext context;
        private readonly IReqnrollOutputHelper outputWriter;

        public EtwIngestSteps(ScenarioContext context, IReqnrollOutputHelper outputWriter)
        {
            this.context = context;
            this.outputWriter = outputWriter;
        }

        [TechTalk.SpecFlow.Given(@"etl file ""[^""]*""")]
        public void GivenEtlFile(string etlFile)
        {
            this.outputWriter.WriteLine($"etl file: {etlFile}");
            this.context.Set(etlFile, "etlFile");
        }

        [TechTalk.SpecFlow.When(@"I parse etl file")]
        public void WhenEtlFileIsExtracted()
        {
            var etlFile = this.context.Get<string>("etlFile");
            var etl = new EtlFile(etlFile);
            var etwEvents = etl.Parse();
            this.context.Set(etwEvents, "etwEvents");
        }

        [TechTalk.SpecFlow.Then(@"the result have the following events")]
        public void ThenTheResultHaveTheFollowingEvents(Table table)
        {
            var etwEvents = this.context.Get<Dictionary<(string providerName, string eventName), EtwEvent>>("etwEvents");
            foreach (var row in table.Rows)
            {
                var providerName = row["ProviderName"];
                var eventName = row["EventName"];
                etwEvents.ContainsKey((providerName, eventName)).Should().BeTrue();
            }
        }
    }
}