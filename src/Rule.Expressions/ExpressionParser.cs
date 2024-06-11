// -----------------------------------------------------------------------
// <copyright file="ExpressionParser.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions
{
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;

    public class ExpressionParser
    {
        private static readonly JsonSerializerSettings MediaTypeFormatterSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            DateParseHandling = DateParseHandling.None,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Error,

            Converters = new List<JsonConverter>
            {
                new StringEnumConverter {NamingStrategy = new CamelCaseNamingStrategy()},
                new ConditionExpressionConverter()
            }
        };

        private static readonly JsonSerializer JsonMediaTypeSerializer =
            JsonSerializer.Create(MediaTypeFormatterSettings);

        public static IConditionExpression Parse(JToken rawFilter)
        {
            return rawFilter.ToObject<IConditionExpression>(JsonMediaTypeSerializer)!;
        }
    }
}