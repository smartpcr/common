// -----------------------------------------------------------------------
// <copyright file="PropertyExtension.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Newtonsoft.Json;

    public static class PropertyExtension
    {
        public static PropertyInfo GetMappedProperty(this Type? type, string fieldName)
        {
            var properties = type.GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.GetProperty |
                BindingFlags.GetField);
            var match = properties.SingleOrDefault(info =>
            {
                var propertyName = info.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? info.Name;
                return string.Equals(propertyName, fieldName, StringComparison.OrdinalIgnoreCase);
            });

            if (match == null)
            {
                match = properties.Single(info => string.Equals(info.Name, fieldName, StringComparison.OrdinalIgnoreCase));
            }

            return match;
        }
    }
}