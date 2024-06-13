// -----------------------------------------------------------------------
// <copyright file="UnixEpochTimeConverter.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Shared;

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

/// <summary>
/// Converts a <see cref="DateTime"/> to and from Unix epoch time
/// </summary>
public class UnixEpochTimeConverter : DateTimeConverterBase
{
    internal static readonly DateTime UnixEpoch = DateTime.UnixEpoch;

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        long seconds;
        if (value is DateTime dateTime)
        {
            seconds = (long)(dateTime.ToUniversalTime() - UnixEpoch).TotalSeconds;
        }
        else
        {
            throw new JsonSerializationException("Expected date object value.");
        }

        if (seconds < 0)
        {
            seconds = 0;
        }

        writer.WriteValue(seconds);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        bool nullable = Nullable.GetUnderlyingType(objectType) != null;
        if (reader.TokenType == JsonToken.Null)
        {
            if (!nullable)
            {
                throw new JsonSerializationException($"Cannot convert null value to {objectType}.");
            }

            return null;
        }

        long seconds;
        if (reader.TokenType == JsonToken.Integer)
        {
            seconds = (long)reader.Value!;
        }
        else if (reader.TokenType == JsonToken.String)
        {
            if (!long.TryParse((string)reader.Value!, out seconds))
            {
                throw new JsonSerializationException($"Cannot convert invalid value {reader.Value} to {objectType}.");
            }
        }
        else
        {
            throw new JsonSerializationException($"Unexpected token parsing date. Expected Integer or String, got {reader.TokenType}.");
        }

        if (seconds >= 0)
        {
            DateTime d = UnixEpoch.AddSeconds(seconds);
            return d;
        }

        return DateTime.UnixEpoch;
    }
}