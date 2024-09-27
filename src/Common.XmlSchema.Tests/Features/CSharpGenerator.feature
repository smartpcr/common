Feature: CSharpGenerator
	As a user,
  I want to be able to generate C# code from an XML schema

Scenario: generate csharp code from a simple schema
	Given xsd schema file "TestData\\DiscoveryManifest.xsd"
	When I generate csharp code with namespace "Microsoft.AzureStack.Services.Update.ResourceProvider.Discovery.UnitTest.Schema.DiscoveryManifest" to output folder "E:\\work\\hci\\update\\src\\UpdateResourceProvider\\UpdateService\\Discovery\\UnitTest\\Schema\\DiscoveryManifest"
  Then the code should be generated to "E:\\work\\hci\\update\\src\\UpdateResourceProvider\\UpdateService\\Discovery\\UnitTest\\Schema\\DiscoveryManifest"
  | FileName |
  | Enums.cs |
  | DiscoveryManifest.cs |
