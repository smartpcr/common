// -----------------------------------------------------------------------
// <copyright file="DataReaderExtension.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Shared;

using System;
using System.Data;
using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

public static class DataReaderExtension
{
    public static T? Value<T>(this IDataReader reader, string fieldName)
    {
        return reader[fieldName] == DBNull.Value
            ? default
            : (T)reader[fieldName];
    }

    public static T? Value<T>(this IDataReader reader, int columnIndex)
    {
        return reader[columnIndex] == DBNull.Value
            ? default
            : (T)reader[columnIndex];
    }

    public static T? EnumValue<T>(this IDataReader reader, int columnIndex)
    {
        if (reader[columnIndex] == DBNull.Value)
        {
            return default;
        }

        var stringValue = reader.Value<string>(columnIndex);
        if (Enum.TryParse(typeof(T), stringValue, true, out var parsedValue) && parsedValue is T enumValue)
        {
            return enumValue;
        }

        return default;
    }

    public static object? EnumValue(this IDataReader reader, int columnIndex, Type enumType)
    {
        if (reader[columnIndex] == DBNull.Value)
        {
            return default;
        }

        var stringValue = reader.Value<string>(columnIndex);
        if (Enum.TryParse(enumType, stringValue, true, out var parsedValue) && parsedValue?.GetType() == enumType)
        {
            return parsedValue;
        }

        return null;
    }

    public static T EnumValue<T>(this IDataReader reader, string fieldName, ILogger logger, T defaultValue)
    {
        if (reader[fieldName] == DBNull.Value)
        {
            return defaultValue;
        }

        var stringValue = reader.Value<string>(fieldName);
        if (Enum.TryParse(typeof(T), stringValue, true, out var parsedValue) && parsedValue is T enumValue)
        {
            return enumValue;
        }

        logger.ReadEnumError(typeof(T).Name, stringValue ?? "empty");
        return defaultValue;
    }

    public static T? EnumValue<T>(this IDataReader reader, int columnIndex, ILogger logger, T defaultValue)
    {
        if (reader[columnIndex] == DBNull.Value)
        {
            return default;
        }

        var stringValue = reader.Value<string>(columnIndex);
        if (Enum.TryParse(typeof(T), stringValue, true, out var parsedValue) && parsedValue is T enumValue)
        {
            return enumValue;
        }

        logger.ReadEnumError(typeof(T).Name, stringValue ?? "empty");
        return defaultValue;
    }

    public static T?[] JsonArray<T>(this IDataReader reader, string fieldName)
    {
        if (reader[fieldName] is JObject)
        {
            return Array.Empty<T>();
        }

        if (reader[fieldName] is JArray array)
        {
            if (array.Count > 0)
            {
                return array.Select(a => a.Value<T>()).ToArray();
            }
        }

        return Array.Empty<T>();
    }

    public static T?[] JsonArray<T>(this IDataReader reader, int columnIndex)
    {
        if (reader[columnIndex] is JObject)
        {
            return Array.Empty<T>();
        }

        if (reader[columnIndex] is JArray array)
        {
            if (array.Count > 0)
            {
                return array.Select(a => a.Value<T>()).ToArray();
            }
        }

        return Array.Empty<T>();
    }
}