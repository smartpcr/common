// -----------------------------------------------------------------------
// <copyright file="TraceTestSteps.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Tests.Steps
{
    using Reqnroll;

    [Binding]
    public class TraceTestSteps
    {
        private readonly ScenarioContext context;

        public TraceTestSteps(ScenarioContext context)
        {
            this.context = context;

        }

        [Given("otlp trace file at {otlpTraceFile}")]
        public void GivenOtlpTraceFileAt(string otlpTraceFile)
        {
            this.context.Pending();
        }

        [When("I export the trace to a temp folder {tempTraceFolder}")]
        public void WhenIExportTheTraceToATempFile(string tempTraceFolder)
        {
            this.context.Pending();
        }

        [Then("the temp files should exist")]
        public void ThenTheTempFileShouldExist()
        {
            this.context.Pending();
        }
    }
}