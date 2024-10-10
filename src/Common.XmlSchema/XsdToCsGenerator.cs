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
        private readonly XDocument xsdSchema;

        public XsdToCsGenerator(string xsdSchemaFilePath)
        {
            this.xsdSchema = XDocument.Load(xsdSchemaFilePath);
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
            File.WriteAllText(Path.Combine(outputFolderPath, $"Enums.cs"), enumBuilder.ToString());

            var complexTypes = this.xsdSchema.Descendants().Where(e => e.Name.LocalName == "complexType");
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

            // Check if this complex type extends another complex type
            var complexContent = complexType.Element(xs + "complexContent");
            var extension = complexContent?.Element(xs + "extension");
            string baseClassName = null;
            if (extension != null)
            {
                // The base class is specified in the 'base' attribute of <xs:extension>
                baseClassName = extension.Attribute("base")?.Value;
            }

            stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}[XmlRoot(\"{className}\")]");
            IEnumerable<XElement> childElements = null;
            IEnumerable<XElement> attributes = null;
            if (baseClassName != null)
            {
                // Generate a class that inherits from the base class
                stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}public partial class {className} : {baseClassName}{Environment.NewLine}{Indent(indent)}{{");
                childElements = extension.Element(xs + "sequence")?.Elements() ?? extension.Element(xs + "choice")?.Elements();
                attributes = extension.Elements().Where(e => e.Name.LocalName == "attribute");
            }
            else
            {
                // Generate a class without inheritance
                stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}public partial class {className}{Environment.NewLine}{Indent(indent)}{{");
                childElements = complexType.Element(xs + "sequence")?.Elements() ?? complexType.Element(xs + "choice")?.Elements();
                attributes = complexType.Elements().Where(e => e.Name.LocalName == "attribute");
            }

            indent++; // properties
            // Process the elements (sequence or choice) inside the complex type
            var elements = childElements?.Where(e => e.Name.LocalName == "element").ToList();
            if (elements != null)
            {
                foreach (var element in elements)
                {
                    GenerateProperty(element, enumTypes, regexPatterns, stringBuilder, indent);
                }
            }

            // Process the attributes inside the complex type
            foreach (var attribute in attributes)
            {
                GenerateProperty(attribute, enumTypes, regexPatterns, stringBuilder, indent, isAttribute: true);
            }

            indent--; // class
            stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}}}");
            indent--; // namespace
            stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}}}");
            File.WriteAllText(Path.Combine(outputFolderPath, $"{className}.cs"), stringBuilder.ToString());
        }

        private void GenerateProperty(
            XElement element,
            HashSet<string> enumTypes,
            Dictionary<string, string> regexPatterns,
            StringBuilder stringBuilder,
            int indent,
            bool isAttribute = false)
        {
            var propertyName = element.Attribute("name")?.Value;
            if (propertyName == null) return;

            var (propertyType, isNullable) = GetPropertyType(element);
            var isRequired = isAttribute
                ? element.Attribute("use")?.Value == "required"
                : element.Attribute("minOccurs")?.Value == "1";
            var isList = !isAttribute && element.Attribute("maxOccurs")?.Value == "unbounded";
            var fixedValue = element.Attribute("fixed")?.Value;
            var defaultValue = element.Attribute("default")?.Value;

            if (!isRequired && !string.IsNullOrEmpty(fixedValue))
            {
                stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}[XmlIgnore]");
            }
            else if (isRequired)
            {
                stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}[Required]{(isAttribute ? "[XmlAttribute(\"" : "[XmlElement(\"")}{propertyName}\")]");
            }
            else
            {
                stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}{(isAttribute ? "[XmlAttribute(\"" : "[XmlElement(\"")}{propertyName}\")]");
            }

            if (regexPatterns.TryGetValue(propertyType, out var regexPattern))
            {
                stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}[RegularExpression(@\"{regexPattern}\")]");
                propertyType = "string";
            }

            var typeName = $"{(isList ? "List<" : "")}{propertyType}{(isList ? ">" : "")}";
            stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}public {typeName} {propertyName} {{ get; set; }}");

            defaultValue = fixedValue ?? defaultValue;
            if (!string.IsNullOrEmpty(defaultValue))
            {
                if (propertyType == "string")
                {
                    defaultValue = $"\"{defaultValue}\"";
                }
                else if (propertyType == "bool")
                {
                    defaultValue = defaultValue.ToLowerInvariant();
                }
                else if (propertyType == "DateTime")
                {
                    defaultValue = $"DateTime.Parse(\"{defaultValue}\")";
                }
                stringBuilder.Append($" = {defaultValue};");
            }
            stringBuilder.AppendLine();

            if ((enumTypes.Contains(propertyType) || isNullable) && !isRequired && string.IsNullOrEmpty(fixedValue))
            {
                stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}[XmlIgnore]");
                stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}public bool {propertyName}Specified {{ get; set; }}{Environment.NewLine}");
            }
        }

        private string Indent(int indent)
        {
            return new string(' ', indent * 4);
        }

        private void GenerateEnums(StringBuilder stringBuilder, int indent, HashSet<string> enumTypes)
        {
            var simpleTypes = xsdSchema.Descendants().Where(e => e.Name.LocalName == "simpleType");
            foreach (var simpleType in simpleTypes)
            {
                var restriction = simpleType.Elements().FirstOrDefault(e => e.Name.LocalName == "restriction" && e.Attribute("base")?.Value == "xs:string");
                if (restriction != null)
                {
                    string enumName = simpleType.Attribute("name")!.Value;
                    var enumerations = restriction.Elements().Where(e => e.Name.LocalName == "enumeration").ToList();

                    if (!string.IsNullOrEmpty(enumName) && enumerations.Any())
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
            var simpleTypes = xsdSchema.Descendants().Where(e => e.Name.LocalName == "simpleType");
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
            string xsdType = element.Attribute("type")?.Value;
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
            return string.Join("_", value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                .Replace("-", "_")
                .Replace(".", "")
                .Replace(",", "")
                .Replace("(", "")
                .Replace(")", "");
        }
    }
}