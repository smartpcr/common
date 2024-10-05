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

    public static List<(string columnName, Type columnType)> GetColumns(this Type type)
    {
        var allProps = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var columns = allProps.Select(prop =>
        {
            var jsonPropAttr = prop.GetCustomAttribute<JsonPropertyAttribute>();
            var propName = jsonPropAttr?.PropertyName ?? prop.Name;
            return (propName, prop.PropertyType);
        }).ToList();

        return columns;
    }

    public static List<ColumnMapping> GetKustoColumnMappings(this Type type)
    {
        var allProps = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var columnMappings = allProps.Select(prop =>
        {
            var jsonPropAttr = prop.GetCustomAttribute<JsonPropertyAttribute>();
            var propName = jsonPropAttr?.PropertyName ?? prop.Name;
            var kustoColAttr = prop.GetCustomAttribute<KustoColumnAttribute>();
            var kustoFieldType = kustoColAttr?.CslType ?? prop.PropertyType.ToKustoColumnType();
            var transformMethod = (prop.PropertyType == typeof(string[]) || prop.PropertyType == typeof(List<string>))
                ? TransformationMethod.PropertyBagArrayToDictionary
                : TransformationMethod.None;
            return new ColumnMapping
            {
                ColumnName = propName,
                ColumnType = kustoFieldType,
                Properties = new Dictionary<string, string>
                {
                    { "Path", "$." + propName },
                    { "Transform", transformMethod.ToString() }
                }
            };
        }).ToList();

        return columnMappings;
    }
}