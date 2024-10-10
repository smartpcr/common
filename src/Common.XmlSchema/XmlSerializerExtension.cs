// -----------------------------------------------------------------------
// <copyright file="XmlSerializerExtension.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.XmlSchema
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using System.Xml.Schema;
    using System.Xml.Serialization;

    public static class XmlSerializerExtension
    {
        private static readonly XNamespace xs = "http://www.w3.org/2001/XMLSchema";

        public static T DeserializeXml<T>(this string filePath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using FileStream fileStream = new FileStream(filePath, FileMode.Open);
            return (T)serializer.Deserialize(fileStream);
        }

        public static void SerializeXml<T>(this T obj, string filePath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using FileStream fileStream = new FileStream(filePath, FileMode.Create);
            serializer.Serialize(fileStream, obj);
        }

        public static void AddToSchemas(this string startingXsdFilePath, XmlSchemaSet schemas, HashSet<string> processedXsdFiles)
        {
            if (processedXsdFiles.Contains(startingXsdFilePath))
            {
                return;
            }

            var xsdDocument = XDocument.Load(startingXsdFilePath);
            schemas.Add(null, startingXsdFilePath);
            processedXsdFiles.Add(startingXsdFilePath);
            var startingFolder = Path.GetDirectoryName(startingXsdFilePath);

            // Find all <xs:include> and <xs:import> elements
            var includeAndImportElements = xsdDocument.Descendants()
                .Where(e => e.Name == xs + "include" || e.Name == xs + "import")
                .ToList();

            foreach (var element in includeAndImportElements)
            {
                // Get the schemaLocation attribute
                string schemaLocation = element.Attribute("schemaLocation")?.Value;

                if (!string.IsNullOrEmpty(schemaLocation))
                {
                    string fullPath = Path.Combine(startingFolder!, schemaLocation);
                    if (File.Exists(fullPath))
                    {
                        fullPath.AddToSchemas(schemas, processedXsdFiles);
                    }
                }
            }
        }
    }
}