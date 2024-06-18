// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JsonFixtureFile.cs" company="Microsoft Corporation">
//   Copyright (c) 2020 Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Rule.Expressions.Tests.Data
{
    using System;
    using System.IO;
    using System.Reflection;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;

    internal class JsonFixtureFile
    {
        public const string FileFixturesDirectoryName = "Data";

        private static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private readonly string path;

        public JsonFixtureFile(string path)
        {
            this.path = Path.Combine(
                path1: Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                path2: FileFixturesDirectoryName,
                path3: path);
            if (!File.Exists(this.path))
            {
                throw new ArgumentException(this.path);
            }
        }

        public string Text => File.ReadAllText(this.path);

        public JToken JToken => JToken.Parse(this.Text);

        public T JObjectOf<T>() => JsonConvert.DeserializeObject<T>(this.Text, serializerSettings)!;

        public object JObjectOf(Type type) => JsonConvert.DeserializeObject(this.Text, type, serializerSettings)!;

        public string PropertyValue(string name) => this.JToken[name]!.Value<string>()!;
    }
}