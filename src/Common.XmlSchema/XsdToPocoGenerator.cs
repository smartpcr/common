﻿// -----------------------------------------------------------------------
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

    public class XsdToPocoGenerator
    {
        private static readonly XNamespace xs = "http://www.w3.org/2001/XMLSchema";
        private readonly XDocument _xsdSchema;

        public XsdToPocoGenerator(string xsdSchemaFilePath)
        {
            this._xsdSchema = XDocument.Load(xsdSchemaFilePath);
        }

        public void GeneratePocoClasses(string outputFolderPath, string @namespace)
        {
            if (!Directory.Exists(outputFolderPath))
            {
                Directory.CreateDirectory(outputFolderPath);
            }

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
                this.GenerateCodeForComplexType(complexType, @namespace, enumTypes, outputFolderPath);
            }
        }

        private void GenerateCodeForComplexType(XElement complexType, string @namespace, HashSet<string> enumTypes, string outputFolderPath)
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

            var elements = complexType.Element(xs + "sequence")?.Elements().Where(e => e.Name.LocalName == "element").ToList();
            if (elements != null)
            {
                foreach (var element in elements)
                {
                    string propertyName = element.Attribute("name")!.Value;
                    string propertyType = GetPropertyType(element);
                    bool isRequired = element.Attribute("minOccurs")?.Value == "1";
                    bool isList = element.Attribute("maxOccurs")?.Value == "unbounded";
                    if (enumTypes.Contains(propertyType) && !isRequired)
                    {
                        propertyType += "?";
                    }
                    string typeName = $"{(isList ? "List<" : "")}{propertyType}{(isList ? ">" : "")}";

                    if (isRequired)
                    {
                        stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}[Required][XmlElement(\"{propertyName}\")]");
                    }
                    else
                    {
                        stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}[XmlElement(\"{propertyName}\")]");
                    }

                    stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}public {typeName} {propertyName} {{ get; set; }}{Environment.NewLine}");
                }
            }

            var attributes = complexType.Elements().Where(e => e.Name.LocalName == "attribute");
            foreach (var attribute in attributes)
            {
                string attrName = attribute.Attribute("name")!.Value;
                bool isRequired = attribute.Attribute("use")?.Value == "required";
                string attrType = GetPropertyType(attribute);
                if (enumTypes.Contains(attrType) && !isRequired)
                {
                    attrType += "?";
                }

                if (isRequired)
                {
                    stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}[Required][XmlAttribute(\"{attrName}\")]");
                }
                else
                {
                    stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}[XmlAttribute(\"{attrName}\")]");
                }
                stringBuilder.Append($"{Environment.NewLine}{Indent(indent)}public {attrType} {attrName} {{ get; set; }}{Environment.NewLine}");
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
                    if (!string.IsNullOrEmpty(enumName))
                    {
                        stringBuilder.Append($"{Environment.NewLine}{this.Indent(indent)}public enum {enumName}");
                        stringBuilder.Append($"{Environment.NewLine}{this.Indent(indent)}{{");


                        var enumerations = restriction.Elements().Where(e => e.Name.LocalName == "enumeration");
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

        private string GetPropertyType(XElement element)
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
                        return complexTypeName;
                    }
                }

                var simpleType = element.Element(element.Name.Namespace + "simpleType");
                if (simpleType != null)
                {
                    var simpleTypeName = simpleType.Attribute("name")?.Value ?? element.Attribute("name")?.Value;
                    if (!string.IsNullOrEmpty(simpleTypeName))
                    {
                        return simpleTypeName;
                    }
                }

                return "string"; // Default to string for unhandled types
            }

            if (!xsdType.StartsWith("xs:"))
            {
                return xsdType;
            }

            switch (xsdType)
            {
                case "xs:string":
                    return "string";
                case "xs:int":
                case "xs:integer":
                    return "int";
                case "xs:unsignedInt":
                    return "uint";
                case "xs:long":
                    return "long";
                case "xs:decimal":
                    return "decimal";
                case "xs:date":
                    return "DateTime";
                case "xs:boolean":
                    return "bool";
                default:
                    return "string"; // Default to string for unhandled types
            }
        }

        private string ToEnumName(string value)
        {
            // Convert values into valid C# enum member names (e.g., replace spaces, ensure valid format)
            return string.Join("_", value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Replace("-", "_")
                .Replace(".", "")
                .Replace(",", "")
                .Replace("(", "")
                .Replace(")", "");
        }
    }
}