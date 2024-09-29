// -----------------------------------------------------------------------
// <copyright file="CodeGeneratorSteps.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.XmlSchema.Tests.Steps
{
    using FluentAssertions;
    using Reqnroll;
    using TestSchema;

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

        [Given(@"input xml file ""(.*)""")]
        public void GivenInputXmlFile(string xmlFilePath)
        {
            this.context.Set(xmlFilePath, "InputXmlFilePath");
        }

        [When(@"I generate csharp code with namespace ""(.*)"" to output folder ""(.*)""")]
        public void GenerateCodeWithNamespace(string @namespace, string outputFolder)
        {
            var xsdFilePath = this.context.Get<string>("XsdSchemaFilePath");
            var gen = new XsdToCsGenerator(xsdFilePath);
            gen.GeneratePocoClasses(outputFolder, @namespace);
        }

        [When(@"I instantiate from xml file")]
        public void IInstantiateFromXmlFile()
        {
            var xmlFilePath = this.context.Get<string>("InputXmlFilePath");
            var updateDiscoveryManifest = xmlFilePath.DeserializeXml<UpdateDiscoveryManifest>();
            this.context.Set(updateDiscoveryManifest, "UpdateDiscoveryManifest");
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

        [Then(@"the object should be deserialized successfully")]
        public void ThenTheObjectShouldBeDeserializedSuccessfully()
        {
            var updateDiscoveryManifest = this.context.Get<UpdateDiscoveryManifest>("UpdateDiscoveryManifest");
            updateDiscoveryManifest.Should().NotBeNull();
        }
    }
}