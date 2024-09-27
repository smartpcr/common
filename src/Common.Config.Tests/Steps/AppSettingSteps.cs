// -----------------------------------------------------------------------
// <copyright file="AppSettingSteps.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Config.Tests.Steps;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Hooks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models;
using Newtonsoft.Json.Linq;
using Reqnroll;
using TechTalk.SpecFlow.Assist;

[Binding]
public class AppSettingSteps
{
    private readonly ScenarioContext scenarioContext;

    public AppSettingSteps(ScenarioContext scenarioContext)
    {
        this.scenarioContext = scenarioContext;
    }

    [Given(@"I have a valid ""(.*)"" file")]
    public void GivenIHaveAValidFile(string fileName)
    {
        var logger = this.scenarioContext.GetLogger<AppSettingSteps>();
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
        logger.LogInformation($"appsettings file path: {filePath}");
        File.Exists(filePath).Should().BeTrue();
    }

    [When(@"I get the appsettings")]
    public void WhenIGetTheAppsettings()
    {
        var config = this.scenarioContext.GetConfiguration();
        config.Should().NotBeNull();
        var logger = this.scenarioContext.GetLogger<AppSettingSteps>();
        logger.LogInformation($"appsettings is loaded: \n{config}");
        this.scenarioContext.Set(config);
    }

    [Then(@"I should get the following values")]
    public void ThenIShouldGetTheFollowingValues(Table table)
    {
        var config = this.scenarioContext.Get<IConfiguration>();
        foreach (var row in table.Rows)
        {
            var key = row["Key"];
            var value = row["Value"];
            config[key].Should().Be(value);
        }
    }

    [Given(@"setting file containing ""(.*)""")]
    public void GivenSettingFileContaining(string key)
    {
        var logger = ScenarioContextExtension.GetLogger<AppSettingSteps>(scenarioContext);
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
        logger.LogInformation($"appsettings file path: {filePath}");
        File.Exists(filePath).Should().BeTrue();
        var config = JObject.Parse(File.ReadAllText(filePath));
        config.ContainsKey(key).Should().BeTrue();
    }

    [Then(@"I should get key vault")]
    public void ThenIShouldGetKeyVault(Table table)
    {
        var config = this.scenarioContext.Get<IConfiguration>();
        var kvSettings = config.GetConfiguredSettings<KeyVaultSettings>();
        var expected = table.CreateInstance<KeyVaultSettings>();
        expected.Should().NotBeNull();
        kvSettings.Should().BeEquivalentTo(expected);
    }

    [Then(@"I should get connection")]
    public void ThenIShouldGetConnection()
    {
        var config = this.scenarioContext.Get<IConfiguration>();
        try
        {
            config.GetConfiguredSettings<Connection>();
            scenarioContext.Set(true, "validation");
        }
        catch (InvalidOperationException ex)
        {
            scenarioContext.Set(false, "validation");
            scenarioContext.Set(ex.Message, "validation_errors");
        }
    }

    [Then(@"I should get validation errors")]
    public void ThenIShouldGetValidationErrors(Table table)
    {
        scenarioContext.Get<bool>("validation").Should().BeFalse();
        var allErrors = scenarioContext.Get<string>("validation_errors");
        var regex = new Regex("DataAnnotation validation failed for members: '(\\w+)' with the error: '([^']+)'.", RegexOptions.Compiled);
        var matches = regex.Matches(allErrors);
        var errors = new List<(string fieldName, string errorMessage)>();
        foreach (Match match in matches)
        {
            errors.Add((match.Groups[1].Value, match.Groups[2].Value));
        }

        errors.Count.Should().Be(table.RowCount);
        foreach (var row in table.Rows)
        {
            var fieldName = row["PropertyName"];
            var errorMessage = row["ErrorMessage"];
            var validationError = errors.FirstOrDefault(e => e.fieldName == fieldName);
            validationError.Should().NotBeNull();
            validationError.errorMessage.Should().Be(errorMessage);
        }
    }

    [Then(@"I should NOT get any validation errors")]
    public void ThenIShouldNotGetAnyValidationErrors()
    {
        scenarioContext.Get<bool>("validation").Should().BeTrue();
    }
}