// -----------------------------------------------------------------------
// <copyright file="KustoExtension.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Kusto;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using global::Kusto.Cloud.Platform.Utils;
using global::Kusto.Data.Common;
using Newtonsoft.Json;

public static class KustoExtension
{
    public static string ToKustoColumnType(this Type type)
    {
        var typeToKustoColumnType = new Dictionary<Type, string>
        {
            { typeof(string), "string" },
            { typeof(bool), "bool" },
            { typeof(bool?), "bool" },
            { typeof(DateTime), "datetime" },
            { typeof(DateTime?), "datetime" },
            { typeof(DateTimeOffset), "datetime" },
            { typeof(DateTimeOffset?), "datetime" },
            { typeof(Guid), "guid" },
            { typeof(Guid?), "guid" },
            { typeof(int), "int" },
            { typeof(byte), "int" },
            { typeof(int?), "int" },
            { typeof(long), "long" },
            { typeof(long?), "long" },
            { typeof(decimal), "real" },
            { typeof(decimal?), "real" },
            { typeof(float), "real" },
            { typeof(float?), "real" },
            { typeof(double), "real" },
            { typeof(double?), "real" },
            { typeof(TimeSpan), "timespan" }
        };

        if (type.IsEnum)
        {
            return "string";
        }

        if (!type.IsScalar())
        {
            return "dynamic";
        }

        return typeToKustoColumnType.GetValueOrDefault(type, "string");
    }

    public static List<(JsonColumnMapping mapping, Type fieldType)> GetKustoColumnMappings(this Type type)
    {
        var columnMappings = new List<(JsonColumnMapping mapping, Type fieldType)>();
        var declaredProps = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        var allProps = type.GetProperties();
        if (declaredProps.Any())
        {
            var nonDeclaredProps = allProps.Where(p => declaredProps.All(p2 => p2.Name != p.Name)).ToArray();
            allProps = declaredProps.Union(nonDeclaredProps).ToArray();
        }

        foreach (var prop in allProps)
        {
            var jsonPropAttr = prop.GetCustomAttribute<JsonPropertyAttribute>();
            var propName = jsonPropAttr?.PropertyName ?? prop.Name;
            var kustoColAttr = prop.GetCustomAttribute<KustoColumnAttribute>();
            var kustoFieldType = kustoColAttr == null
                ? prop.PropertyType.ToKustoColumnType()
                : kustoColAttr.CslType;
            var propType = prop.PropertyType.GetTypeWithNullableSupport();
            if (propType == typeof(decimal))
            {
                propType = typeof(double);
            }

            if (prop.PropertyType == typeof(string[]) || prop.PropertyType == typeof(List<string>))
            {
                columnMappings.Add((
                    new JsonColumnMapping
                    {
                        ColumnName = propName,
                        ColumnType = kustoFieldType,
                        JsonPath = "$." + propName,
                        TransformationMethod = TransformationMethod.PropertyBagArrayToDictionary
                    }, typeof(string)));
            }
            else
            {
                columnMappings.Add((new JsonColumnMapping
                {
                    ColumnName = propName,
                    ColumnType = kustoFieldType,
                    JsonPath = "$." + propName,
                }, propType));
            }
        }

        return columnMappings;
    }
}