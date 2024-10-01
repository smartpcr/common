// -----------------------------------------------------------------------
// <copyright file="XsdToPocoGenerator.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.XmlSchema
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;

    /// <summary>
    /// TODO: handle xsd import, scan for global and inline complex type, only create new class if contents not matching
    /// ignore difference among xs:choice, xs:all and xs:sequence, c# doesn't support it
    /// </summary>
    public class XsdToCsGenerator
    {
        private static readonly XNamespace xs = "http://www.w3.org/2001/XMLSchema";
        private readonly XDocument _xsdSchema;

        public XsdToCsGenerator(string xsdSchemaFilePath)
        {
            this._xsdSchema = XDocument.Load(xsdSchemaFilePath);
        }

        public void GeneratePocoClasses(string outputFolderPath, string @namespace)
        {
            if (!Directory.Exists(outputFolderPath))
            {
                Directory.CreateDirectory(outputFolderPath);
            }

            var patterns = new Dictionary<string, string>();
            this.PopulateStringRegexPatterns(patterns);
            var enumBuilder = new StringBuilder();
            enumBuilder.AppendLine(@$"//--------------------------------------------------------------
// <copyright file=""Enums.cs"" company=""Microsoft Corp."">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
//--------------------------------------------------------------
");
            var enumTypes = new HashSet<string>();
            var indent = 0;
            enumBuilder.Append($"{Indent(indent)}namespace {@namespace}{Environment.NewLine}{{");
            indent++; // class
            this.GenerateEnums(enumBuilder, indent, enumTypes);
            indent--; // namespace
            enumBuilder.Append($"{Environment.NewLine}{Indent(indent)}}}");
            System.IO.File.WriteAllText(Path.Combine(outputFolderPath, $"Enums.cs"), enumBuilder.ToString());

            var complexTypes = this._xsdSchema.Descendants().Where(e => e.Name.LocalName == "complexType");
            foreach (var complexType in complexTypes)
            {
                this.GenerateCodeForComplexType(complexType, @namespace, enumTypes, patterns, outputFolderPath);
            }
        }

        private void GenerateCodeForComplexType(XElement complexType, string @namespace, HashSet<string> enumTypes, Dictionary<string, string> regexPatterns, string outputFolderPath)
        {
            var indent = 0;
            var className = complexType.Attribute("name")?.Value ?? complexType.Parent?.Attribute("name")?.Value;
            if (className == null)
            {
                return;
            }

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(@$"//--------------------------------------------------------------
// <copyright file=""{className}.cs"" company=""Microsoft Corp."">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
//--------------------------------------------------------------
");
            stringBuilder.Append($"{Indent(indent)}namespace {@namespace}{Environment.NewLine}{{");
            indent++; // class
            stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}using System;");
            stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}using System.Collections.Generic;");
            stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}using System.ComponentModel.DataAnnotations;");
            stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}using System.Xml.Serialization;{Environment.NewLine}");

            stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}[XmlRoot(\"{className}\")]");
            stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}public partial class {className}{Environment.NewLine}{Indent(indent)}{{");
            indent++; // properties

            var childElements = complexType.Element(xs + "sequence")?.Elements() ?? complexType.Element(xs + "choice")?.Elements();
            var elements = childElements?.Where(e => e.Name.LocalName == "element").ToList();
            if (elements != null)
            {
                foreach (var element in elements)
                {
                    string propertyName = element.Attribute("name")!.Value;
                    var (propertyType, isNullable) = GetPropertyType(element);
                    bool isRequired = element.Attribute("minOccurs")?.Value == "1";
                    bool isList = element.Attribute("maxOccurs")?.Value == "unbounded";

                    if (isRequired)
                    {
                        stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}[Required][XmlElement(\"{propertyName}\")]");
                    }
                    else
                    {
                        stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}[XmlElement(\"{propertyName}\")]");
                    }

                    if (regexPatterns.TryGetValue(propertyType, out var regexPattern))
                    {
                        stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}[RegularExpression(@\"{regexPattern}\")]");
                        propertyType = "string";
                    }

                    string typeName = $"{(isList ? "List<" : "")}{propertyType}{(isList ? ">" : "")}";
                    stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}public {typeName} {propertyName} {{ get; set; }}{Environment.NewLine}");

                    if ((enumTypes.Contains(propertyType) || isNullable) && !isRequired)
                    {
                        stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}[XmlIgnore]");
                        stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}public bool {propertyName}Specified {{ get; set; }}{Environment.NewLine}");
                    }
                }
            }

            var attributes = complexType.Elements().Where(e => e.Name.LocalName == "attribute");
            foreach (var attribute in attributes)
            {
                string attrName = attribute.Attribute("name")!.Value;
                bool isRequired = attribute.Attribute("use")?.Value == "required";
                var (attrType, isNullable) = GetPropertyType(attribute);

                if (isRequired)
                {
                    stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}[Required][XmlAttribute(\"{attrName}\")]");
                }
                else
                {
                    stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}[XmlAttribute(\"{attrName}\")]");
                }

                if (regexPatterns.TryGetValue(attrType, out var regexPattern))
                {
                    stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}[RegularExpression(@\"{regexPattern}\")]");
                    attrType = "string";
                }

                stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}public {attrType} {attrName} {{ get; set; }}{Environment.NewLine}");

                if ((enumTypes.Contains(attrType) || isNullable) && !isRequired)
                {
                    stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}[XmlIgnore]");
                    stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}public bool {attrName}Specified {{ get; set; }}{Environment.NewLine}");
                }
            }

            indent--; // class
            stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}}}");
            indent--; // namespace
            stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}}}");
            System.IO.File.WriteAllText(Path.Combine(outputFolderPath, $"{className}.cs"), stringBuilder.ToString());
        }

        private string Indent(int indent)
        {
            return new string(' ', indent * 4);
        }

        private void GenerateEnums(StringBuilder stringBuilder, int indent, HashSet<string> enumTypes)
        {
            var simpleTypes = _xsdSchema.Descendants().Where(e => e.Name.LocalName == "simpleType");
            foreach (var simpleType in simpleTypes)
            {
                var restriction = simpleType.Elements().FirstOrDefault(e => e.Name.LocalName == "restriction" && e.Attribute("base")?.Value == "xs:string");
                if (restriction != null)
                {
                    string enumName = simpleType.Attribute("name")!.Value;
                    var enumerations = restriction.Elements().Where(e => e.Name.LocalName == "enumeration").ToList();

                    if (!string.IsNullOrEmpty(enumName) && enumerations?.Any() == true)
                    {
                        stringBuilder.Append($"{Environment.NewLine}{this.Indent(indent)}public enum {enumName}");
                        stringBuilder.Append($"{Environment.NewLine}{this.Indent(indent)}{{");
                        foreach (var enumeration in enumerations)
                        {
                            string enumValue = enumeration.Attribute("value")?.Value ?? enumeration.Value;
                            if (!string.IsNullOrEmpty(enumValue))
                            {
                                enumValue = this.ToEnumName(enumValue);
                                stringBuilder.Append($"{Environment.NewLine}{this.Indent(indent + 1)}{enumValue},");
                            }
                        }
                        stringBuilder.Append($"{Environment.NewLine}{this.Indent(indent)}}}{Environment.NewLine}");
                        enumTypes.Add(enumName);
                    }
                }
            }
        }

        private void PopulateStringRegexPatterns(Dictionary<string, string> patterns)
        {
            var simpleTypes = _xsdSchema.Descendants().Where(e => e.Name.LocalName == "simpleType");
            foreach (var simpleType in simpleTypes)
            {
                var restriction = simpleType.Elements().FirstOrDefault(e => e.Name.LocalName == "restriction" && e.Attribute("base")?.Value == "xs:string");
                if (restriction != null)
                {
                    var pattern = restriction.Elements().FirstOrDefault(e => e.Name.LocalName == "pattern");
                    if (pattern != null)
                    {
                        string patternValue = pattern.Attribute("value")?.Value;
                        if (!string.IsNullOrEmpty(patternValue))
                        {
                            string typeName = simpleType.Attribute("name")!.Value;
                            patterns.Add(typeName, patternValue);
                        }
                    }
                }
            }
        }

        private (string typeName, bool isNullable) GetPropertyType(XElement element)
        {
            string? xsdType = element.Attribute("type")?.Value;
            if (string.IsNullOrEmpty(xsdType))
            {
                var complexType = element.Element(element.Name.Namespace + "complexType");
                if (complexType != null)
                {
                    var complexTypeName = complexType.Attribute("name")?.Value ?? element.Attribute("name")?.Value;
                    if (!string.IsNullOrEmpty(complexTypeName))
                    {
                        return (complexTypeName, false);
                    }
                }

                var simpleType = element.Element(element.Name.Namespace + "simpleType");
                if (simpleType != null)
                {
                    var simpleTypeName = simpleType.Attribute("name")?.Value ?? element.Attribute("name")?.Value;
                    if (!string.IsNullOrEmpty(simpleTypeName))
                    {
                        return (simpleTypeName, true);
                    }
                }

                return ("string", false); // Default to string for unhandled types
            }

            if (!xsdType.StartsWith("xs:"))
            {
                return (xsdType, false);
            }

            switch (xsdType)
            {
                case "xs:string":
                    return ("string", false);
                case "xs:int":
                case "xs:integer":
                    return ("int", true);
                case "xs:unsignedInt":
                    return ("uint", true);
                case "xs:long":
                    return ("long", true);
                case "xs:decimal":
                    return ("decimal", true);
                case "xs:boolean":
                    return ("bool", true);
                case "xs:dateTime":
                case "xs:date":
                    return ("DateTime", true);
                default:
                    return ("string", false); // Default to string for unhandled types
            }
        }

        private string ToEnumName(string value)
        {
            // Convert values into valid C# enum member names (e.g., replace spaces, ensure valid format)
            return string.Join("_", value.Split(new []{' '}, StringSplitOptions.RemoveEmptyEntries))
                .Replace("-", "_")
                .Replace(".", "")
                .Replace(",", "")
                .Replace("(", "")
                .Replace(")", "");
        }
    }
}