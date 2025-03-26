// -----------------------------------------------------------------------
// <copyright file="UnzipSteps.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Tests.Steps
{
    using System;
    using System.IO;
    using System.Linq;
    using ETW;
    using FluentAssertions;
    using Reqnroll;
    using Reqnroll.Infrastructure;

    [Binding]
    public class UnzipSteps
    {
        private readonly ScenarioContext context;
        private readonly IReqnrollOutputHelper outputWriter;

        public UnzipSteps(ScenarioContext context, IReqnrollOutputHelper outputWriter)
        {
            this.context = context;
            this.outputWriter = outputWriter;
        }

        [Given("Given one or more zip files in folder \"([^\"]+)\"")]
        public void GivenGivenOneOrMoreZipFilesInFolder(string zipFolder)
        {
            Directory.Exists(zipFolder).Should().BeTrue();
            var zipFiles = Directory.GetFiles(zipFolder, "*.zip", SearchOption.TopDirectoryOnly);
            zipFiles.Should().NotBeNullOrEmpty();
            this.outputWriter.WriteLine($"total of {zipFiles.Length} zip files found");
            this.context.Set(zipFolder, "zipFolder");
        }

        [When("I extract zip files to collect etl files to folder \"([^\"]+)\"")]
        public void WhenIExtractZipFilesToCollectEtlFilesToFolder(string etlFolder)
        {
            if (!Directory.Exists(etlFolder))
            {
                Directory.CreateDirectory(etlFolder);
            }

            var zipFolder = this.context.Get<string>("zipFolder");
            var zipFiles = Directory.GetFiles(zipFolder, "*.zip", SearchOption.TopDirectoryOnly);
            foreach (var zipFile in zipFiles)
            {
                var unzipHelper = new UnzipHelper(zipFile, etlFolder, "etl");
                unzipHelper.Process();
            }
        }

        [Then("I should see all etl files in folder \"([^\"]+)\"")]
        public void ThenIShouldSeeAllEtlFilesInFolder(string etlFolder)
        {
            Directory.Exists(etlFolder).Should().BeTrue();
            var etlFiles = Directory.GetFiles(etlFolder, "*.etl", SearchOption.AllDirectories);
            etlFiles.Should().NotBeNullOrEmpty();
        }

        [Given(@"A zip file at ""(.+)""")]
        public void GivenAZipFileAt(string zipFile)
        {
            if (zipFile.Contains("%HOME%", StringComparison.OrdinalIgnoreCase))
            {
                zipFile = zipFile.Replace("%HOME%", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            }
            File.Exists(zipFile).Should().BeTrue();
            this.context.Set(zipFile, "zipFile");
        }

        [When(@"I extract ""(.+)"" files from zip file to folder ""(.+)""")]
        public void WhenIExtractZipFileToFolder(string fileExt, string extractFolder)
        {
            if (extractFolder.Contains("%HOME%", StringComparison.OrdinalIgnoreCase))
            {
                extractFolder = extractFolder.Replace("%HOME%", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            }

            if (!Directory.Exists(extractFolder))
            {
                Directory.CreateDirectory(extractFolder);
            }
            var zipFile = this.context.Get<string>("zipFile");
            var unzipHelper = new UnzipHelper(zipFile, extractFolder, fileExt);
            unzipHelper.Process();
        }

        [Then(@"I should see the following ""(.+)"" files in folder ""(.+)""")]
        public void ThenIShouldSeeTheFollowingFilesInFolder(string fileExt, string outputFolder, Table table)
        {
            if (outputFolder.Contains("%HOME%", StringComparison.OrdinalIgnoreCase))
            {
                outputFolder = outputFolder.Replace("%HOME%", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            }

            Directory.Exists(outputFolder).Should().BeTrue();
            var files = Directory.GetFiles(outputFolder, $"*.{fileExt}", SearchOption.AllDirectories);
            files.Should().NotBeNullOrEmpty();
            var fileNames = files.Select(Path.GetFileName).ToList();
            fileNames.Count.Should().BeGreaterThanOrEqualTo(table.Rows.Count);

            foreach (var row in table.Rows)
            {
                var etlFile = row["FileName"];
                fileNames.Contains(etlFile, StringComparer.OrdinalIgnoreCase).Should().BeTrue();
            }
        }
    }
}