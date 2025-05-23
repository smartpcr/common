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
namespace Common.Monitoring.Tests.Features
{
    using Reqnroll;
    using System;
    using System.Linq;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Reqnroll", "2.0.0.0")]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public partial class IngestHCIEtwAndEvtxEventsFeature : object, Xunit.IClassFixture<IngestHCIEtwAndEvtxEventsFeature.FixtureData>, Xunit.IAsyncLifetime
    {
        
        private global::Reqnroll.ITestRunner testRunner;
        
        private static string[] featureTags = ((string[])(null));
        
        private static global::Reqnroll.FeatureInfo featureInfo = new global::Reqnroll.FeatureInfo(new System.Globalization.CultureInfo("en-US"), "Features", "ingest HCI etw and evtx events", null, global::Reqnroll.ProgrammingLanguage.CSharp, featureTags);
        
        private Xunit.Abstractions.ITestOutputHelper _testOutputHelper;
        
#line 1 "IngestHci.feature"
#line hidden
        
        public IngestHCIEtwAndEvtxEventsFeature(IngestHCIEtwAndEvtxEventsFeature.FixtureData fixtureData, Xunit.Abstractions.ITestOutputHelper testOutputHelper)
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
        
        public virtual async System.Threading.Tasks.Task FeatureBackgroundAsync()
        {
#line 2
  #line hidden
#line 3
    await testRunner.GivenAsync("kusto cluster uri \"http://192.168.23.191:8080\"", ((string)(null)), ((global::Reqnroll.Table)(null)), "Given ");
#line hidden
#line 4
    await testRunner.AndAsync("kusto database name \"hci\"", ((string)(null)), ((global::Reqnroll.Table)(null)), "And ");
#line hidden
#line 5
    await testRunner.AndAsync("kustainer volume mount from \"E:\\\\kustodata\" to \"/kustodata\"", ((string)(null)), ((global::Reqnroll.Table)(null)), "And ");
#line hidden
        }
        
        async System.Threading.Tasks.Task Xunit.IAsyncLifetime.InitializeAsync()
        {
            await this.TestInitializeAsync();
        }
        
        async System.Threading.Tasks.Task Xunit.IAsyncLifetime.DisposeAsync()
        {
            await this.TestTearDownAsync();
        }
        
        [Xunit.SkippableFactAttribute(DisplayName="end to end ingestion of HCI logs")]
        [Xunit.TraitAttribute("FeatureTitle", "ingest HCI etw and evtx events")]
        [Xunit.TraitAttribute("Description", "end to end ingestion of HCI logs")]
        public async System.Threading.Tasks.Task EndToEndIngestionOfHCILogs()
        {
            string[] tagsOfScenario = ((string[])(null));
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            global::Reqnroll.ScenarioInfo scenarioInfo = new global::Reqnroll.ScenarioInfo("end to end ingestion of HCI logs", null, tagsOfScenario, argumentsOfScenario, featureTags);
#line 7
  this.ScenarioInitialize(scenarioInfo);
#line hidden
            if ((global::Reqnroll.TagHelper.ContainsIgnoreTag(scenarioInfo.CombinedTags) || global::Reqnroll.TagHelper.ContainsIgnoreTag(featureTags)))
            {
                testRunner.SkipScenario();
            }
            else
            {
                await this.ScenarioStartAsync();
#line 2
  await this.FeatureBackgroundAsync();
#line hidden
#line 8
    await testRunner.GivenAsync("A zip file at \"%HOME%\\\\Downloads\\\\hci.zip\"", ((string)(null)), ((global::Reqnroll.Table)(null)), "Given ");
#line hidden
#line 9
    await testRunner.WhenAsync("I extract \"etl\" files from zip file to folder \"%HOME%\\\\Downloads\\\\hci\\\\etw\"", ((string)(null)), ((global::Reqnroll.Table)(null)), "When ");
#line hidden
                global::Reqnroll.Table table3 = new global::Reqnroll.Table(new string[] {
                            "FileName"});
                table3.AddRow(new string[] {
                            "V-HOST1_AzureStack.Update.Admin.2024-10-09.1.etl"});
#line 10
    await testRunner.ThenAsync("I should see the following \"etl\" files in folder \"%HOME%\\\\Downloads\\\\hci\\\\etw\"", ((string)(null)), table3, "Then ");
#line hidden
#line 13
    await testRunner.WhenAsync("I extract \"evtx\" files from zip file to folder \"%HOME%\\\\Downloads\\\\hci\\\\evtx\"", ((string)(null)), ((global::Reqnroll.Table)(null)), "When ");
#line hidden
                global::Reqnroll.Table table4 = new global::Reqnroll.Table(new string[] {
                            "FileName"});
                table4.AddRow(new string[] {
                            "Event_Microsoft.AzureStack.LCMController.EventSource-Admin.evtx"});
#line 14
    await testRunner.ThenAsync("I should see the following \"evtx\" files in folder \"%HOME%\\\\Downloads\\\\hci\\\\evtx\"", ((string)(null)), table4, "Then ");
#line hidden
#line 17
    await testRunner.WhenAsync("I parse etl files in folder \"%HOME%\\\\Downloads\\\\hci\\\\etw\"", ((string)(null)), ((global::Reqnroll.Table)(null)), "When ");
#line hidden
#line 18
    await testRunner.ThenAsync("I should find 121 distinct events in etl files", ((string)(null)), ((global::Reqnroll.Table)(null)), "Then ");
#line hidden
#line 19
    await testRunner.WhenAsync("I create tables based on etl event schemas", ((string)(null)), ((global::Reqnroll.Table)(null)), "When ");
#line hidden
                global::Reqnroll.Table table5 = new global::Reqnroll.Table(new string[] {
                            "TableName"});
                table5.AddRow(new string[] {
                            "ETL-Microsoft-URP-InfraEventSource.HealthCheckResultDirectoryIsEmptyOrDoesNotExis" +
                                "t"});
                table5.AddRow(new string[] {
                            "ETL-Microsoft-URP-InfraEventSource.ResolverGetAll"});
                table5.AddRow(new string[] {
                            "ETL-Microsoft-URP-InfraEventSource.StopService"});
#line 20
    await testRunner.ThenAsync("I should see following etl kusto tables", ((string)(null)), table5, "Then ");
#line hidden
#line 25
    await testRunner.WhenAsync("I extract etl files in folder \"%HOME%\\\\Downloads\\\\hci\\\\etw\" to csv files in folde" +
                        "r \"%HOME%\\\\Downloads\\\\hci\\\\csv\"", ((string)(null)), ((global::Reqnroll.Table)(null)), "When ");
#line hidden
                global::Reqnroll.Table table6 = new global::Reqnroll.Table(new string[] {
                            "FileName"});
                table6.AddRow(new string[] {
                            "ETL-Microsoft-URP-InfraEventSource.ResolverGetAll.csv"});
                table6.AddRow(new string[] {
                            "ETL-Microsoft-URP-InfraEventSource.StartService.csv"});
                table6.AddRow(new string[] {
                            "ETL-Microsoft-URP-InfraEventSource.StopService.csv"});
#line 26
    await testRunner.ThenAsync("I should see following csv files in folder \"%HOME%\\\\Downloads\\\\hci\\\\csv\"", ((string)(null)), table6, "Then ");
#line hidden
#line 31
    await testRunner.WhenAsync("I parse evtx files in folder \"%HOME%\\\\Downloads\\\\hci\\\\evtx\"", ((string)(null)), ((global::Reqnroll.Table)(null)), "When ");
#line hidden
#line 32
    await testRunner.ThenAsync("I should find 4475 distinct records in evtx files", ((string)(null)), ((global::Reqnroll.Table)(null)), "Then ");
#line hidden
#line 33
    await testRunner.WhenAsync("I create table based on evtx record schema", ((string)(null)), ((global::Reqnroll.Table)(null)), "When ");
#line hidden
                global::Reqnroll.Table table7 = new global::Reqnroll.Table(new string[] {
                            "TableName"});
                table7.AddRow(new string[] {
                            "WindowsEvents"});
#line 34
    await testRunner.ThenAsync("I should see following evtx kusto table", ((string)(null)), table7, "Then ");
#line hidden
#line 37
    await testRunner.WhenAsync("I extract evtx records to csv files in folder \"%HOME%\\\\Downloads\\\\hci\\\\csv\"", ((string)(null)), ((global::Reqnroll.Table)(null)), "When ");
#line hidden
#line 38
    await testRunner.ThenAsync("I should see following csv file \"WindowsEvents.csv\" in folder \"%HOME%\\\\Downloads\\" +
                        "\\hci\\\\csv\"", ((string)(null)), ((global::Reqnroll.Table)(null)), "Then ");
#line hidden
#line 39
    await testRunner.WhenAsync("I ingest csv files in folder \"%HOME%\\\\Downloads\\\\hci\\\\csv\" to kusto", ((string)(null)), ((global::Reqnroll.Table)(null)), "When ");
#line hidden
                global::Reqnroll.Table table8 = new global::Reqnroll.Table(new string[] {
                            "TableName",
                            "RecordCount"});
                table8.AddRow(new string[] {
                            "WindowsEvents",
                            "4475"});
#line 40
    await testRunner.ThenAsync("the following kusto tables should have added records with expected counts", ((string)(null)), table8, "Then ");
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
                await IngestHCIEtwAndEvtxEventsFeature.FeatureSetupAsync();
            }
            
            async System.Threading.Tasks.Task Xunit.IAsyncLifetime.DisposeAsync()
            {
                await IngestHCIEtwAndEvtxEventsFeature.FeatureTearDownAsync();
            }
        }
    }
}
#pragma warning restore
#endregion
