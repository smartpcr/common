// -----------------------------------------------------------------------
// <copyright file="IngestHciSteps.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Tests.Steps
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Kusto;
    using Config.Tests.Hooks;
    using Common.Monitoring.ETW;
    using FluentAssertions;
    using global::Kusto.Data.Common;
    using TechTalk.SpecFlow;
    using TechTalk.SpecFlow.Infrastructure;

    [Binding]
    public class IngestHciSteps
    {
        private readonly ScenarioContext context;
        private readonly ISpecFlowOutputHelper outputWriter;

        public IngestHciSteps(ScenarioContext context, ISpecFlowOutputHelper outputWriter)
        {
            this.context = context;
            this.outputWriter = outputWriter;
        }

        [When("I parse etl files in folder \"([^\"]+)\"")]
        public void WhenIParseEtlFilesInFolder(string etlFolder)
        {
            etlFolder = this.NormalizeFolderPath(etlFolder);
            var etlFiles = Directory.GetFiles(etlFolder, "*.etl", SearchOption.TopDirectoryOnly);
            this.outputWriter.WriteInfo($"Found {etlFiles.Length} etl files in {etlFolder}");
            var allEtwEvents = new ConcurrentDictionary<(string providerName, string eventName), EtwEvent>();

            // NOTE: etw parser is not thread safe, so we need to parse etl files one by one
            foreach (var etlFile in etlFiles)
            {
                this.outputWriter.WriteVerbose($"Parsing {etlFile}");
                var etl = new EtlFile(etlFile);
                var etwEvents = new ConcurrentDictionary<(string providerName, string eventName), EtwEvent>();
                var failedToParse = false;
                etl.Parse(etwEvents, ref failedToParse);
                if (failedToParse)
                {
                    this.outputWriter.WriteError($"Failed to parse {etlFile}");
                }
                else
                {
                    this.outputWriter.WriteVerbose($"Parsed {etwEvents.Count} events from file {etlFile}");
                    foreach (var key in etwEvents.Keys)
                    {
                        allEtwEvents.TryAdd(key, etwEvents[key]);
                    }
                }
            }

            this.context.Set(allEtwEvents, "etlEventSchemas");
            this.outputWriter.WriteInfo($"Parsed {allEtwEvents.Count} events from all files");
        }

        [Then(@"I should find (\d+) distinct events in etl files")]
        public void ThenIShouldFindDistinctEventsInEtlFiles(int expectedCount)
        {
            var etlEventSchemas = this.context.Get<ConcurrentDictionary<(string providerName, string eventName), EtwEvent>>("etlEventSchemas");
            etlEventSchemas.Count.Should().Be(expectedCount);
        }

        [When(@"I create tables based on etl event schemas")]
        public void WhenICreateTablesBasedOnEtlEventSchemas()
        {
            var etlEventSchemas = this.context.Get<ConcurrentDictionary<(string providerName, string eventName), EtwEvent>>("etlEventSchemas");
            var adminClient = this.context.Get<ICslAdminProvider>("adminClient");
            var queryClient = this.context.Get<ICslQueryProvider>("queryClient");
            var allKustoTableNames = new ConcurrentBag<string>();
            var parallelOpts = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
            var processed = 0;
            var dbName = this.context.Get<string>("dbName");
            var etwTableExisingRecordCount = new Dictionary<string, long>();

            Parallel.ForEach(etlEventSchemas.Keys, parallelOpts, key =>
            {
                var (providerName, eventName) = key;
                var kustoTableName = $"ETL-{providerName}.{eventName.Replace("/", "")}";
                allKustoTableNames.Add(kustoTableName);
                try
                {
                    if (!adminClient.IsTableExist(kustoTableName))
                    {
                        var eventFields = etlEventSchemas[(providerName, eventName)].PayloadSchema;
                        var createTableCmd = KustoExtension.GenerateCreateTableCommand(kustoTableName, eventFields);
                        adminClient.ExecuteControlCommand(createTableCmd);
                        this.outputWriter.WriteVerbose($"Table {kustoTableName} created");

                        // create ingestion mapping
                        var csvMappingCmd = KustoExtension.GenerateCsvIngestionMapping(kustoTableName, "CsvMapping", eventFields);
                        adminClient.ExecuteControlCommand(csvMappingCmd);
                        this.outputWriter.WriteVerbose($"Ingestion mapping for {kustoTableName} created");
                    }
                    else
                    {
                        this.outputWriter.WriteVerbose($"kusto table {kustoTableName} already exists, skip creating it");

                        var recordCountCmd = $"['{kustoTableName}'] | count";
                        using var reader = queryClient.ExecuteQuery(dbName, recordCountCmd, new ClientRequestProperties());
                        if (reader.Read())
                        {
                            var recordCount = reader.GetInt64(0);
                            etwTableExisingRecordCount.Add(kustoTableName, recordCount);
                        }
                    }

                    allKustoTableNames.Add(kustoTableName);
                }
                catch (Exception ex)
                {
                    this.outputWriter.WriteError($"Error: failed to create kusto table {kustoTableName}, error: {ex.Message}");
                }
                finally
                {
                    var currentProcessed = Interlocked.Increment(ref processed);
                    if (currentProcessed % 10 == 0)
                    {
                        this.outputWriter.WriteVerbose($"Processed {currentProcessed} of {etlEventSchemas.Count} kusto tables");
                    }
                }
            });

            this.context.Set(allKustoTableNames, "etlKustoTableNames");
            this.context.Set(etwTableExisingRecordCount, "etwTableExisingRecordCount");
        }

        [Then(@"I should see following etl kusto tables")]
        public void ThenIShouldSeeFollowingEtlKustoTables(Table table)
        {
            var etlKustoTableNames = this.context.Get<ConcurrentBag<string>>("etlKustoTableNames");
            etlKustoTableNames.Count.Should().BeGreaterOrEqualTo(table.Rows.Count);

            foreach (var row in table.Rows)
            {
                etlKustoTableNames.Should().Contain(row[0]);
            }
        }

        [When(@"I extract etl files in folder ""([^""]+)"" to csv files in folder ""([^""]+)""")]
        public void WhenIExtractEtlFilesInFolderToCsvFolder(string etlFolder, string csvFolder)
        {
            etlFolder = this.NormalizeFolderPath(etlFolder);
            var etlFiles = Directory.GetFiles(etlFolder, "*.etl", SearchOption.TopDirectoryOnly);
            this.outputWriter.WriteInfo($"Found {etlFiles.Length} etl files in {etlFolder}");

            csvFolder = this.NormalizeFolderPath(csvFolder);
            if (!Directory.Exists(csvFolder))
            {
                Directory.CreateDirectory(csvFolder);
            }

            var etlKustoTableNames = this.context.Get<ConcurrentBag<string>>("etlKustoTableNames");
            var etlEventSchemas = this.context.Get<ConcurrentDictionary<(string providerName, string eventName), EtwEvent>>("etlEventSchemas");

            var totalCsvFileGenerated = 0;
            var totalCsvFileScanned = 0;
            foreach (var kvp in etlEventSchemas)
            {
                totalCsvFileScanned++;
                var kustoTableName = $"ETL-{kvp.Key.providerName}.{kvp.Key.eventName.Replace("/", "")}";
                if (!etlKustoTableNames.Contains(kustoTableName))
                {
                    continue;
                }
                var csvFileName = Path.Combine(csvFolder, $"{kustoTableName}.csv");
                if (!File.Exists(csvFileName))
                {
                    var fieldNames = kvp.Value.PayloadSchema.Select(f => f.fieldName).ToList();
                    var columnHeader = string.Join(',', fieldNames) + Environment.NewLine;
                    File.WriteAllText(csvFileName, columnHeader);
                    totalCsvFileGenerated++;
                }
                this.outputWriter.WriteVerbose($"scanned {totalCsvFileScanned} events, generated {totalCsvFileGenerated} csv files");
            }

            var processed = 0;
            var successfulIngests = 0;
            var failedIngests = 0;
            var allFileContents = new ConcurrentDictionary<(string providerName, string eventName), List<string>>();
            var fileRecords = new ConcurrentDictionary<string, int>();

            foreach (var etlFile in etlFiles)
            {
                var etl = new EtlFile(etlFile);
                try
                {
                    var fileContents = etl.Process(etlEventSchemas.ToDictionary(p => p.Key, p => p.Value));
                    foreach (var kvp in fileContents)
                    {
                        allFileContents.AddOrUpdate(kvp.Key, kvp.Value, (_, existingValue) =>
                        {
                            existingValue.AddRange(kvp.Value);
                            return existingValue;
                        });
                    }

                    Interlocked.Increment(ref successfulIngests);
                }
                catch (Exception ex)
                {
                    this.outputWriter.WriteError($"failed to extract events from : {etlFile},: {ex.Message}");
                    Interlocked.Increment(ref failedIngests);
                }
                finally
                {
                    var currentProcessed = Interlocked.Increment(ref processed);
                    if (currentProcessed % 10 == 0)
                    {
                        this.outputWriter.WriteVerbose($"Generating csv files from etl files, processed {currentProcessed} of {etlFiles.Length} etl files");
                    }
                }
            }

            foreach(var kvp in allFileContents)
            {
                var csvFileName = $"ETL-{kvp.Key.providerName}.{kvp.Key.eventName.Replace("/", "")}.csv";
                var csvFilePath = Path.Combine(csvFolder, csvFileName);
                if (File.Exists(csvFilePath) && kvp.Value.Count > 0)
                {
                    File.AppendAllLines(csvFilePath, kvp.Value);
                    fileRecords.AddOrUpdate(csvFileName, kvp.Value.Count, (_, existingValue) => existingValue + kvp.Value.Count);
                }
            }

            allFileContents.Clear();
            this.outputWriter.WriteInfo($"Finished generating csv files, total generated: {successfulIngests}, total failed: {failedIngests}");
            foreach (var csvFileName in fileRecords.Keys)
            {
                this.outputWriter.WriteVerbose($"Generated {fileRecords[csvFileName]} records in {csvFileName}");
            }
        }

        [Then(@"I should see following csv files in folder ""([^""]+)""")]
        public void ThenIShouldSeeFollowingCsvFilesInFolder(string csvFolder, Table table)
        {
            csvFolder = this.NormalizeFolderPath(csvFolder);
            var csvFiles = Directory.GetFiles(csvFolder, "*.csv", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName).ToList();
            csvFiles.Should().NotBeNullOrEmpty();
            csvFiles.Count.Should().BeGreaterOrEqualTo(table.Rows.Count);

            foreach (var row in table.Rows)
            {
                csvFiles.Should().Contain(row[0]);
            }
        }

        [When(@"I parse evtx files in folder ""([^""]+)""")]
        public void WhenIParseEvtxFilesInFolder(string evtxFolder)
        {
            evtxFolder = this.NormalizeFolderPath(evtxFolder);
            var evtxFiles = Directory.GetFiles(evtxFolder, "*.evtx", SearchOption.TopDirectoryOnly);
            this.outputWriter.WriteInfo($"Found {evtxFiles.Length} evtx files in {evtxFolder}");
            var allEvtxRecords = new ConcurrentBag<EvtxRecord>();

            Parallel.ForEach(evtxFiles,
                evtxFile =>
                {
                    var evtxParser = new EvtxFileParser(evtxFile);
                    var records = evtxParser.Parse();
                    foreach (var record in records)
                    {
                        allEvtxRecords.Add(record);
                    }
                });

            this.context.Set(allEvtxRecords, "evtxRecords");
            this.outputWriter.WriteInfo($"Parsed {allEvtxRecords.Count} records from {evtxFiles.Length} evtx files");
        }

        [Then(@"I should find (\d+) distinct records in evtx files")]
        public void ThenIShouldFindDistinctEventsInEvtxFiles(int expectedCount)
        {
            var evtxRecords = this.context.Get<ConcurrentBag<EvtxRecord>>("evtxRecords");
            evtxRecords.Count.Should().Be(expectedCount);
        }

        [When(@"I create table based on evtx record schema")]
        public async Task WhenICreateTableBasedOnEvtxRecordSchema()
        {
            var adminClient = this.context.Get<ICslAdminProvider>("adminClient");
            var queryClient = this.context.Get<ICslQueryProvider>("queryClient");
            var kustoTableName = "WindowsEvents";
            var eventFields = new List<(string fieldName, Type fieldType)>
            {
                (nameof(EvtxRecord.TimeStamp), typeof(DateTime)),
                (nameof(EvtxRecord.ProviderName), typeof(string)),
                (nameof(EvtxRecord.LogName), typeof(string)),
                (nameof(EvtxRecord.MachineName), typeof(string)),
                (nameof(EvtxRecord.EventId), typeof(int)),
                (nameof(EvtxRecord.Level), typeof(string)),
                (nameof(EvtxRecord.Opcode), typeof(short?)),
                (nameof(EvtxRecord.Keywords), typeof(string)),
                (nameof(EvtxRecord.ProcessId), typeof(int?)),
                (nameof(EvtxRecord.Description), typeof(string)),
            };
            var dbName = this.context.Get<string>("dbName");
            var evtxTableExisingRecordCount = new Dictionary<string, long>();

            if (!adminClient.IsTableExist(kustoTableName))
            {
                var createTableCmd = KustoExtension.GenerateCreateTableCommand(kustoTableName, eventFields);
                adminClient.ExecuteControlCommand(createTableCmd);
                this.outputWriter.WriteInfo($"Table {kustoTableName} created");

                // create ingestion mapping
                var csvMappingCmd = KustoExtension.GenerateCsvIngestionMapping(kustoTableName, "CsvMapping", eventFields);
                adminClient.ExecuteControlCommand(csvMappingCmd);
                this.outputWriter.WriteInfo($"Ingestion mapping for {kustoTableName} created");
            }
            else
            {
                this.outputWriter.WriteVerbose($"kusto table {kustoTableName} already exists, skip creating it");

                var recordCountCmd = $"['{kustoTableName}'] | count";
                using var reader = await queryClient.ExecuteQueryAsync(dbName, recordCountCmd, new ClientRequestProperties());
                if (reader.Read())
                {
                    var recordCount = reader.GetInt64(0);
                    evtxTableExisingRecordCount.Add(kustoTableName, recordCount);
                }
            }

            this.context.Set(kustoTableName, "evtxKustoTableName");
            this.context.Set(evtxTableExisingRecordCount, "evtxTableExisingRecordCount");
        }

        [Then(@"I should see following evtx kusto table")]
        public void ThenIShouldSeeFollowingEvtxKustoTable(Table table)
        {
            var evtxKustoTableName = this.context.Get<string>("evtxKustoTableName");
            table.Rows.Count.Should().Be(1);
            evtxKustoTableName.Should().Be(table.Rows[0][0]);
        }

        [When(@"I extract evtx records to csv files in folder ""([^""]+)""")]
        public void WhenIExtractEvtxRecordsToCsvFilesInFolder(string csvFolder)
        {
            csvFolder = this.NormalizeFolderPath(csvFolder);
            if (!Directory.Exists(csvFolder))
            {
                Directory.CreateDirectory(csvFolder);
            }

            var evtxRecords = this.context.Get<ConcurrentBag<EvtxRecord>>("evtxRecords");
            var csvFileName = Path.Combine(csvFolder, "WindowsEvents.csv");
            if (!File.Exists(csvFileName))
            {
                var columnHeader = "TimeStamp,ProviderName,LogName,MachineName,EventId,Description" + Environment.NewLine;
                File.WriteAllText(csvFileName, columnHeader);
            }

            var csvLines = new List<string>();
            foreach (var evtxRecord in evtxRecords)
            {
                var description = evtxRecord.Description;
                bool containsSpecialCharacters =
                    !string.IsNullOrEmpty(description) && (
                    description.Contains("\"") ||
                    description.Contains(",") ||
                    description.Contains(" ") ||
                    description.Contains("\n") ||
                    description.Contains("\r"));
                if (containsSpecialCharacters)
                {
                    description = description.Replace("\"", "\"\"");
                }
                var csvLine = $"{evtxRecord.TimeStamp:u},{evtxRecord.ProviderName},{evtxRecord.LogName},{evtxRecord.MachineName},{evtxRecord.EventId},{description}";
                csvLines.Add(csvLine);
            }

            File.AppendAllLines(csvFileName, csvLines);

            this.outputWriter.WriteInfo($"Finished populating WindowsEvents.csv files, total records: {csvLines.Count}");
        }

        [Then(@"I should see following csv file ""([^""]+)"" in folder ""([^""]+)""")]
        public void ThenIShouldSeeFollowingCsvFileInFolder(string expectedCsvFile, string csvFolder)
        {
            csvFolder = this.NormalizeFolderPath(csvFolder);
            var csvFileName = Path.Combine(csvFolder, expectedCsvFile);
            File.Exists(csvFileName).Should().BeTrue();
        }

        [When(@"I ingest csv files in folder ""([^""]+)"" to kusto")]
        public void WhenIIngestCsvFilesInFolderToKusto(string csvFolder)
        {
            csvFolder = this.NormalizeFolderPath(csvFolder);
            var adminClient = this.context.Get<ICslAdminProvider>("adminClient");
            var etlEventSchemas = this.context.Get<ConcurrentDictionary<(string providerName, string eventName), EtwEvent>>("etlEventSchemas");
            var volumeBindingHostPath = this.context.Get<string>("hostPath");
            var stagingFolder = Path.Combine(volumeBindingHostPath, "staging");
            if (!Directory.Exists(stagingFolder))
            {
                Directory.CreateDirectory(stagingFolder);
            }
            var volumeBindingContainerPath = this.context.Get<string>("containerPath");
            var tableRecordCount = new Dictionary<string, long>();

            foreach (var (providerName, eventName) in etlEventSchemas.Keys)
            {
                var kustoTableName = $"ETL-{providerName}.{eventName.Replace("/", "")}";
                var csvFileName = Path.Combine(csvFolder, $"{kustoTableName}.csv");
                File.Exists(csvFileName).Should().BeTrue();
                var stagingFileName = Path.Combine(stagingFolder, Path.GetFileName(csvFileName));
                File.Copy(csvFileName, stagingFileName, true);
                var csvFileContainerPath = stagingFileName.Replace(volumeBindingHostPath, volumeBindingContainerPath);
                csvFileContainerPath = csvFileContainerPath.Replace(@"\\", "/");
                csvFileContainerPath = csvFileContainerPath.Replace(@"\", "/");

                var ingestCommand = $".ingest into table ['{kustoTableName}'] (\"{csvFileContainerPath}\") with (format='csv', ingestionMappingReference='CsvMapping', ignoreFirstRecord=true)";
                try
                {
                    adminClient.ExecuteControlCommand(ingestCommand);
                    var fileLineCount = File.ReadLines(csvFileName).Count();
                    tableRecordCount.Add(kustoTableName, fileLineCount - 1); // exclude header
                    this.outputWriter.WriteInfo($"Ingested csv file {csvFileName} to kusto table {kustoTableName}");
                }
                catch (Exception ex)
                {
                    this.outputWriter.WriteError($"Error: failed to ingest csv file {csvFileName} to kusto table {kustoTableName}, error: {ex.Message}");
                }
            }

            var windowsEventsCsvFileName = Path.Combine(csvFolder, "WindowsEvents.csv");
            if (File.Exists(windowsEventsCsvFileName))
            {
                var kustoTableName = "WindowsEvents";
                var stagingFileName = Path.Combine(stagingFolder, Path.GetFileName(windowsEventsCsvFileName));
                File.Copy(windowsEventsCsvFileName, stagingFileName, true);
                var csvFileContainerPath = stagingFileName.Replace(stagingFileName, volumeBindingContainerPath);
                csvFileContainerPath = csvFileContainerPath.Replace(@"\\", "/");
                csvFileContainerPath = csvFileContainerPath.Replace(@"\", "/");

                var ingestCommand = $".ingest into table ['{kustoTableName}'] (\"{csvFileContainerPath}\") with (format='csv', ingestionMappingReference='CsvMapping', ignoreFirstRecord=true)";
                try
                {
                    adminClient.ExecuteControlCommand(ingestCommand);
                    var fileLineCount = File.ReadLines(windowsEventsCsvFileName).Count();
                    tableRecordCount.Add("WindowsEvents", fileLineCount - 1); // exclude header
                    this.outputWriter.WriteInfo($"Ingested csv file {windowsEventsCsvFileName} to kusto table {kustoTableName}");
                }
                catch (Exception ex)
                {
                    this.outputWriter.WriteError($"Error: failed to ingest csv file {windowsEventsCsvFileName} to kusto table {kustoTableName}, error: {ex.Message}");
                }
            }

            this.context.Set(tableRecordCount, "tableRecordCount");
        }

        [Then(@"the following kusto tables should have added records with expected counts")]
        public void ThenTheFollowingKustoTablesShouldHaveAddedRecordsWithExpectedCounts(Table table)
        {
            var tableRecordCount = this.context.Get<Dictionary<string, long>>("tableRecordCount");
            tableRecordCount.Count.Should().BeGreaterOrEqualTo(table.Rows.Count);
            foreach (var row in table.Rows)
            {
                tableRecordCount.Should().ContainKey(row[0]);
                tableRecordCount[row[0]].Should().Be(int.Parse(row[1]));
            }
        }

        private string NormalizeFolderPath(string folderPath)
        {
            if (folderPath.Contains("%HOME%", StringComparison.OrdinalIgnoreCase))
            {
                folderPath = folderPath.Replace("%HOME%", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            }

            return folderPath;
        }
    }
}