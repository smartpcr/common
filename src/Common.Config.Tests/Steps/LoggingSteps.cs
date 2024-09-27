// -----------------------------------------------------------------------
// <copyright file="LoggingSteps.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Config.Tests.Steps;

using System;
using System.Linq;
using Common.Config.Tests.Mocks;
using FluentAssertions;
using Hooks;
using Microsoft.Extensions.Logging;
using Reqnroll;
using TechTalk.SpecFlow.Assist;
using TechTalk.SpecFlow.Infrastructure;

[Binding]
public class LoggingSteps
{
    private readonly ScenarioContext scenarioContext;
    private readonly IReqnrollOutputHelper outputHelper;

    public LoggingSteps(ScenarioContext scenarioContext, IReqnrollOutputHelper outputHelper)
    {
        this.scenarioContext = scenarioContext;
        this.outputHelper = outputHelper;
    }

    [Given(@"logger is configured")]
    public void GivenLoggerIsConfigured()
    {
        outputHelper.WriteLine("logger is configured");
    }

    [When(@"I create several logs")]
    public void WhenICreateSeveralLogs(Table table)
    {
        var logMessages = table.CreateSet<LogMessage>().ToList();
        var logger = scenarioContext.GetLogger<LoggingSteps>();
        foreach (var msg in logMessages)
        {
            logger.Log(msg.Level, msg.Message);
        }
    }

    [Then(@"the following messages should be logged")]
    public void ThenTheFollowingMessagesShouldBeLogged(Table table)
    {
        var logger = scenarioContext.GetLogger<LoggingSteps>();
        var mockedLogger = logger as MockedLogger<LoggingSteps>;
        mockedLogger.Should().NotBeNull();
        mockedLogger!.CategoryName.Should().Be("Common.Config.Tests.Steps.LoggingSteps");
        var loggedMsgs = mockedLogger.Logs;
        loggedMsgs.Count.Should().Be(table.Rows.Count);
        foreach (var row in table.Rows)
        {
            var level = Enum.Parse<LogLevel>(row["Level"]);
            var message = row["Message"];
            loggedMsgs.Any(lm => lm.message == message && lm.level == level).Should().BeTrue();
        }
    }

    internal class LogMessage
    {
        public LogLevel Level { get; set; }
        public string? Message { get; set; }
    }
}