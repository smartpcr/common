﻿// ------------------------------------------------------------------------------
//  <auto-generated>
//      This code was generated by Reqnroll (https://www.reqnroll.net/).
//      Reqnroll Version:2.0.0.0
//      Reqnroll Generator Version:2.0.0.0
// 
//      Changes to this file may cause incorrect behavior and will be lost if
//      the code is regenerated.
//  </auto-generated>
// ------------------------------------------------------------------------------
#region Designer generated code
#pragma warning disable
namespace Common.Config.Tests.Features
{
    using Reqnroll;
    using System;
    using System.Linq;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Reqnroll", "2.0.0.0")]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public partial class LoggingFeature : object, Xunit.IClassFixture<LoggingFeature.FixtureData>, Xunit.IAsyncLifetime
    {
        
        private global::Reqnroll.ITestRunner testRunner;
        
        private static string[] featureTags = ((string[])(null));
        
        private static global::Reqnroll.FeatureInfo featureInfo = new global::Reqnroll.FeatureInfo(new System.Globalization.CultureInfo("en-US"), "Features", "logging", "\tSimple calculator for adding two numbers", global::Reqnroll.ProgrammingLanguage.CSharp, featureTags);
        
        private Xunit.Abstractions.ITestOutputHelper _testOutputHelper;
        
#line 1 "logging.feature"
#line hidden
        
        public LoggingFeature(LoggingFeature.FixtureData fixtureData, Xunit.Abstractions.ITestOutputHelper testOutputHelper)
        {
            this._testOutputHelper = testOutputHelper;
        }
        
        public static async System.Threading.Tasks.Task FeatureSetupAsync()
        {
        }
        
        public static async System.Threading.Tasks.Task FeatureTearDownAsync()
        {
        }
        
        public async System.Threading.Tasks.Task TestInitializeAsync()
        {
            testRunner = global::Reqnroll.TestRunnerManager.GetTestRunnerForAssembly(featureHint: featureInfo);
            if (((testRunner.FeatureContext != null) 
                        && (testRunner.FeatureContext.FeatureInfo.Equals(featureInfo) == false)))
            {
                await testRunner.OnFeatureEndAsync();
            }
            if ((testRunner.FeatureContext == null))
            {
                await testRunner.OnFeatureStartAsync(featureInfo);
            }
        }
        
        public async System.Threading.Tasks.Task TestTearDownAsync()
        {
            await testRunner.OnScenarioEndAsync();
            global::Reqnroll.TestRunnerManager.ReleaseTestRunner(testRunner);
        }
        
        public void ScenarioInitialize(global::Reqnroll.ScenarioInfo scenarioInfo)
        {
            testRunner.OnScenarioInitialize(scenarioInfo);
            testRunner.ScenarioContext.ScenarioContainer.RegisterInstanceAs<Xunit.Abstractions.ITestOutputHelper>(_testOutputHelper);
        }
        
        public async System.Threading.Tasks.Task ScenarioStartAsync()
        {
            await testRunner.OnScenarioStartAsync();
        }
        
        public async System.Threading.Tasks.Task ScenarioCleanupAsync()
        {
            await testRunner.CollectScenarioErrorsAsync();
        }
        
        async System.Threading.Tasks.Task Xunit.IAsyncLifetime.InitializeAsync()
        {
            await this.TestInitializeAsync();
        }
        
        async System.Threading.Tasks.Task Xunit.IAsyncLifetime.DisposeAsync()
        {
            await this.TestTearDownAsync();
        }
        
        [Xunit.SkippableFactAttribute(DisplayName="console logger")]
        [Xunit.TraitAttribute("FeatureTitle", "logging")]
        [Xunit.TraitAttribute("Description", "console logger")]
        [Xunit.TraitAttribute("Category", "unit_test")]
        [Xunit.TraitAttribute("Category", "logging")]
        [Xunit.TraitAttribute("Category", "prod")]
        public async System.Threading.Tasks.Task ConsoleLogger()
        {
            string[] tagsOfScenario = new string[] {
                    "unit_test",
                    "logging",
                    "prod"};
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            global::Reqnroll.ScenarioInfo scenarioInfo = new global::Reqnroll.ScenarioInfo("console logger", null, tagsOfScenario, argumentsOfScenario, featureTags);
#line 5
    this.ScenarioInitialize(scenarioInfo);
#line hidden
            if ((global::Reqnroll.TagHelper.ContainsIgnoreTag(scenarioInfo.CombinedTags) || global::Reqnroll.TagHelper.ContainsIgnoreTag(featureTags)))
            {
                testRunner.SkipScenario();
            }
            else
            {
                await this.ScenarioStartAsync();
#line 6
     await testRunner.GivenAsync("logger is configured", ((string)(null)), ((global::Reqnroll.Table)(null)), "Given ");
#line hidden
                global::Reqnroll.Table table6 = new global::Reqnroll.Table(new string[] {
                            "Level",
                            "Message"});
                table6.AddRow(new string[] {
                            "Debug",
                            "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor in" +
                                "cididunt ut labore et dolore magna aliqua."});
                table6.AddRow(new string[] {
                            "Information",
                            "Pharetra pharetra massa massa ultricies mi quis hendrerit."});
                table6.AddRow(new string[] {
                            "Warning",
                            "Fusce ut placerat orci nulla. Ac ut consequat semper viverra nam."});
                table6.AddRow(new string[] {
                            "Error",
                            "Placerat orci nulla pellentesque dignissim enim sit amet venenatis urna."});
#line 7
        await testRunner.WhenAsync("I create several logs", ((string)(null)), table6, "When ");
#line hidden
                global::Reqnroll.Table table7 = new global::Reqnroll.Table(new string[] {
                            "Level",
                            "Message"});
                table7.AddRow(new string[] {
                            "Debug",
                            "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor in" +
                                "cididunt ut labore et dolore magna aliqua."});
                table7.AddRow(new string[] {
                            "Information",
                            "Pharetra pharetra massa massa ultricies mi quis hendrerit."});
                table7.AddRow(new string[] {
                            "Warning",
                            "Fusce ut placerat orci nulla. Ac ut consequat semper viverra nam."});
                table7.AddRow(new string[] {
                            "Error",
                            "Placerat orci nulla pellentesque dignissim enim sit amet venenatis urna."});
#line 13
        await testRunner.ThenAsync("the following messages should be logged", ((string)(null)), table7, "Then ");
#line hidden
            }
            await this.ScenarioCleanupAsync();
        }
        
        [System.CodeDom.Compiler.GeneratedCodeAttribute("Reqnroll", "2.0.0.0")]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
        public class FixtureData : object, Xunit.IAsyncLifetime
        {
            
            async System.Threading.Tasks.Task Xunit.IAsyncLifetime.InitializeAsync()
            {
                await LoggingFeature.FeatureSetupAsync();
            }
            
            async System.Threading.Tasks.Task Xunit.IAsyncLifetime.DisposeAsync()
            {
                await LoggingFeature.FeatureTearDownAsync();
            }
        }
    }
}
#pragma warning restore
#endregion
