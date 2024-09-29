// -----------------------------------------------------------------------
// <copyright file="XmlSerializerExtension.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.XmlSchema.Tests.Steps
{
    using System.Xml.Serialization;

    public static class XmlSerializerExtension
    {
        public static T? DeserializeXml<T>(this string filePath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using FileStream fileStream = new FileStream(filePath, FileMode.Open);
            return (T?)serializer.Deserialize(fileStream);
        }
    }
}