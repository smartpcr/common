// -----------------------------------------------------------------------
// <copyright file="CodeGeneratorSteps.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.XmlSchema.Tests.Steps
{
    using FluentAssertions;
    using Reqnroll;

    [Binding]
    public class CodeGeneratorSteps
    {
        private readonly ScenarioContext context;

        public CodeGeneratorSteps(ScenarioContext context)
        {
            this.context = context;
        }

        [Given(@"xsd schema file ""(.*)""")]
        public void GivenXsdSchemaFile(string xsdSchemaFilePath)
        {
            this.context.Set(xsdSchemaFilePath, "XsdSchemaFilePath");
        }

        [When(@"I generate csharp code with namespace ""(.*)"" to output folder ""(.*)""")]
        public void GenerateCodeWithNamespace(string @namespace, string outputFolder)
        {
            var xsdFilePath = this.context.Get<string>("XsdSchemaFilePath");
            var gen = new XsdToPocoGenerator(xsdFilePath);
            gen.GeneratePocoClasses(outputFolder, @namespace);
        }

        [Then(@"the code should be generated to ""(.*)""")]
        public void ThenTheGeneratedCodeShouldBeInFolder(string outputFolderName, Table table)
        {
            Directory.Exists(outputFolderName).Should().BeTrue();
            var files = Directory.GetFiles(outputFolderName).Select(Path.GetFileName).ToList();
            foreach (var row in table.Rows)
            {
                var fileName = row["FileName"];
                files.Should().Contain(fileName);
            }
        }
    }
}