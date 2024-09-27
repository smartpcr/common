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
            var enumTypes = new HashSet<string>();
            var indent = 0;
            enumBuilder.Append($"{Indent(indent)}namespace {@namespace}\n{{");
            indent++; // class
            this.GenerateEnums(enumBuilder, indent, enumTypes);
            indent--; // namespace
            enumBuilder.Append($"\n{Indent(indent)}}}");
            System.IO.File.WriteAllText(Path.Combine(outputFolderPath, $"Enums.cs"), enumBuilder.ToString());

            var complexTypes = this._xsdSchema.Descendants().Where(e => e.Name.LocalName == "complexType");
            foreach (var complexType in complexTypes)
            {
                this.GenerateCodeForComplexType(complexType, @namespace, outputFolderPath);
            }
        }

        private void GenerateCodeForComplexType(XElement complexType, string @namespace, string outputFolderPath)
        {
            var indent = 0;
            var className = complexType.Attribute("name")?.Value ?? complexType.Parent?.Attribute("name")?.Value;
            if (className == null)
            {
                return;
            }

            var stringBuilder = new StringBuilder();
            stringBuilder.Append($"{Indent(indent)}namespace {@namespace}\n{{");
            indent++; // class
            stringBuilder.Append($"\n{Indent(indent)}using System;");
            stringBuilder.Append($"\n{Indent(indent)}using System.Collections.Generic;");
            stringBuilder.Append($"\n{Indent(indent)}using System.Xml.Serialization;");
            stringBuilder.Append($"\n{Indent(indent)}using System.ComponentModel.DataAnnotations;\n");

            stringBuilder.Append($"\n{Indent(indent)}[XmlRoot(\"{className}\")]");
            stringBuilder.Append($"\n{Indent(indent)}public class {className}\n{Indent(indent)}{{");
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
                    if (isRequired)
                    {
                        stringBuilder.Append($"\n{Indent(indent)}[Required][XmlElement(\"{propertyName}\")]");
                    }
                    else
                    {
                        stringBuilder.Append($"\n{Indent(indent)}[XmlElement(\"{propertyName}\")]");
                    }
                    stringBuilder.Append($"\n{Indent(indent)}public {(isList ? "List<" : "")}{propertyType}{(isList ? ">" : "")} {propertyName} {{ get; set; }}\n");
                }
            }

            var attributes = complexType.Elements().Where(e => e.Name.LocalName == "attribute");
            foreach (var attribute in attributes)
            {
                string attrName = attribute.Attribute("name")!.Value;
                bool isRequired = attribute.Attribute("use")?.Value == "required";
                string attrType = GetPropertyType(attribute);
                if (isRequired)
                {
                    stringBuilder.Append($"\n{Indent(indent)}[Required][XmlAttribute(\"{attrName}\")]");
                }
                else
                {
                    stringBuilder.Append($"\n{Indent(indent)}[XmlAttribute(\"{attrName}\")]");
                }
                stringBuilder.Append($"\n{Indent(indent)}public {attrType} {attrName} {{ get; set; }}\n");
            }

            indent--; // class
            stringBuilder.Append($"\n{Indent(indent)}}}");
            indent--; // namespace
            stringBuilder.Append($"\n{Indent(indent)}}}");
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
                        stringBuilder.Append($"\n{this.Indent(indent)}public enum {enumName}");
                        stringBuilder.Append($"\n{this.Indent(indent)}{{");


                        var enumerations = restriction.Elements().Where(e => e.Name.LocalName == "enumeration");
                        foreach (var enumeration in enumerations)
                        {
                            string enumValue = enumeration.Attribute("value")?.Value ?? enumeration.Value;
                            if (!string.IsNullOrEmpty(enumValue))
                            {
                                enumValue = this.ToEnumName(enumValue);
                                stringBuilder.Append($"\n{this.Indent(indent + 1)}{enumValue},");
                            }
                        }

                        stringBuilder.Append($"\n{this.Indent(indent)}}}\n");
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