Feature: CSharpGenerator
	As a user,
  I want to be able to generate C# code from an XML schema

Scenario: generate csharp code for SolutionManifest
	Given xsd schema file "Schema\\SolutionManifest.xsd"
	When I generate csharp code with namespace "Microsoft.AzureStack.UpdateService.Models" to output folder "Schema\\DiscoveryManifest\\Models"
  Then the code should be generated to "Schema\\DiscoveryManifest\\Models"
  | FileName    |
  | Enums.cs    |
  | Hotpatch.cs |

Scenario: validate deserialization of generated code
  Given input xml file "TestData\\Valid.xml"
  When I instantiate from xml file
  Then the object should be deserialized successfully
