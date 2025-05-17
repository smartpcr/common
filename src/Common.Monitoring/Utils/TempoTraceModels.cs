// -----------------------------------------------------------------------
// <copyright file="TempoTraceModels.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Serialization;
    using Google.Protobuf;
    using OpenTelemetry.Proto.Trace.V1;

    public class Root
    {
        [JsonPropertyName("batches")] public List<Batch> Batches { get; set; }
    }

    public class Batch
    {
        [JsonPropertyName("resource")] public Resource Resource { get; set; }

        [JsonPropertyName("instrumentationLibrarySpans")]
        public List<InstrumentationLibrarySpans> InstrumentationLibrarySpans { get; set; }
    }

    public class Resource
    {
        [JsonPropertyName("attributes")] public List<KeyValue> Attributes { get; set; }

        [JsonPropertyName("droppedAttributesCount")]
        public uint DroppedAttributesCount { get; set; }
    }

    public class KeyValue
    {
        [JsonPropertyName("key")] public string Key { get; set; }

        [JsonPropertyName("value")] public AttributeValue Value { get; set; }
    }

    public class AttributeValue
    {
        [JsonPropertyName("stringValue")] public string StringValue { get; set; }

        [JsonPropertyName("intValue")] public long? IntValue { get; set; }

        [JsonPropertyName("boolValue")] public bool? BoolValue { get; set; }
    }

    public class InstrumentationLibrarySpans
    {
        [JsonPropertyName("instrumentationLibrary")]
        public InstrumentationLibrary InstrumentationLibrary { get; set; }

        [JsonPropertyName("spans")] public List<SpanRepresentation> Spans { get; set; }
    }

    public class InstrumentationLibrary
    {
        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("version")] public string Version { get; set; }
    }

    public class SpanRepresentation
    {
        [JsonPropertyName("traceId")] public string TraceId { get; set; }

        [JsonPropertyName("spanId")] public string SpanId { get; set; }

        [JsonPropertyName("parentSpanId")] public string ParentSpanId { get; set; }

        [JsonPropertyName("traceState")] public string TraceState { get; set; }

        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("kind")] public string Kind { get; set; }

        [JsonPropertyName("startTimeUnixNano")]
        public ulong StartTimeUnixNano { get; set; }

        [JsonPropertyName("endTimeUnixNano")] public ulong EndTimeUnixNano { get; set; }

        [JsonPropertyName("attributes")] public List<KeyValue> Attributes { get; set; }

        [JsonPropertyName("droppedAttributesCount")]
        public uint DroppedAttributesCount { get; set; }

        [JsonPropertyName("droppedEventsCount")]
        public uint DroppedEventsCount { get; set; }

        [JsonPropertyName("droppedLinksCount")]
        public uint DroppedLinksCount { get; set; }

        [JsonPropertyName("status")] public Status Status { get; set; }

        [JsonPropertyName("events")] public List<EventRepresentation> Events { get; set; }
    }

    public class Status
    {
        [JsonPropertyName("code")] public OpenTelemetry.Proto.Trace.V1.Status.Types.StatusCode Code { get; set; }

        [JsonPropertyName("message")] public string Message { get; set; }
    }

    public class EventRepresentation
    {
        [JsonPropertyName("timeUnixNano")] public ulong TimeUnixNano { get; set; }

        [JsonPropertyName("attributes")] public List<KeyValue> Attributes { get; set; }

        [JsonPropertyName("droppedAttributesCount")]
        public uint DroppedAttributesCount { get; set; }

        [JsonPropertyName("name")] public string Name { get; set; }
    }

    public static class SpanExtensions
    {
        public static SpanRepresentation MapSpan(this Span src)
        {
            string ToHex(ByteString bs) =>
                BitConverter.ToString(bs.ToByteArray()).Replace("-", "").ToLowerInvariant();

            return new SpanRepresentation
            {
                TraceId = ToHex(src.TraceId),
                SpanId = ToHex(src.SpanId),
                ParentSpanId = ToHex(src.ParentSpanId),
                TraceState = src.TraceState,
                Name = src.Name,
                Kind = src.Kind.ToString(), // e.g. Span.Types.SpanKind.Internal
                StartTimeUnixNano = src.StartTimeUnixNano,
                EndTimeUnixNano = src.EndTimeUnixNano,
                Attributes = src.Attributes.Select(a => new KeyValue
                {
                    Key = a.Key,
                    Value = new AttributeValue
                    {
                        StringValue = a.Value.StringValue,
                        BoolValue = a.Value.BoolValue,
                        IntValue = a.Value.IntValue
                    }
                }).ToList(),
                DroppedAttributesCount = src.DroppedAttributesCount,
                DroppedEventsCount = src.DroppedEventsCount,
                DroppedLinksCount = src.DroppedLinksCount,
                Status = new Status
                {
                    Code = src.Status.Code,
                    Message = src.Status.Message
                },
                Events = src.Events?.Select(e => new EventRepresentation
                {
                    TimeUnixNano = e.TimeUnixNano,
                    Attributes = e.Attributes.Select(a => new KeyValue
                    {
                        Key = a.Key,
                        Value = new AttributeValue
                        {
                            StringValue = a.Value.StringValue,
                            BoolValue = a.Value.BoolValue,
                            IntValue = a.Value.IntValue
                        }
                    }).ToList(),
                    DroppedAttributesCount = e.DroppedAttributesCount,
                    Name = e.Name
                }).ToList()
            };
        }
    }
}