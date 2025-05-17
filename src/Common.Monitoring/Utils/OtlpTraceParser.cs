// -----------------------------------------------------------------------
// <copyright file="OtlpTraceParser.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Utils
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using Google.Protobuf;
    using OpenTelemetry.Proto.Collector.Trace.V1;

    public class OtlpTraceParser
    {
        public JsonSerializerOptions Options { get; set; }

        public OtlpTraceParser()
        {
            this.Options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        public List<(string traceId, Root root)> TempoTraceFromOtlpJsonFile(string jsonFile)
        {
            var output = new List<(string traceId, Root)>();
            var parser = new JsonParser(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));
            var allSpans = File
                .ReadLines(jsonFile)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .SelectMany(line =>
                {
                    var req = parser.Parse<ExportTraceServiceRequest>(line);
                    return req.ResourceSpans.SelectMany(rs =>
                    {
                        // build Resource object
                        var resource = new Resource
                        {
                            Attributes = rs.Resource.Attributes.Select(a => new KeyValue
                            {
                                Key = a.Key,
                                Value = new AttributeValue
                                {
                                    StringValue = a.Value.StringValue,
                                    BoolValue   = a.Value.BoolValue,
                                    IntValue    = a.Value.IntValue
                                }
                            }).ToList(),
                            DroppedAttributesCount = rs.Resource.DroppedAttributesCount
                        };

                        return rs.ScopeSpans.SelectMany(sls =>
                            sls.Spans.Select(span => new
                            {
                                Resource = resource,
                                LibName  = sls.Scope.Name,
                                LibVer   = sls.Scope.Version,
                                Span     = span.MapSpan()
                            })
                        );
                    });
                })
                .ToList();

            // Group by traceId
            var grouped = allSpans.GroupBy(x => x.Span.TraceId);

            foreach (var grp in grouped)
            {
                var traceId = grp.Key;
                var root = new Root
                {
                    Batches = grp
                        // group by identical resource
                        .GroupBy(x => JsonSerializer.Serialize(x.Resource, this.Options))
                        .Select(resGroup => new Batch
                        {
                            Resource = resGroup.First().Resource,
                            InstrumentationLibrarySpans = resGroup
                                // then group by library
                                .GroupBy(x => (x.LibName, x.LibVer))
                                .Select(libGroup => new InstrumentationLibrarySpans
                                {
                                    InstrumentationLibrary = new InstrumentationLibrary
                                    {
                                        Name    = libGroup.Key.LibName,
                                        Version = libGroup.Key.LibVer ?? ""
                                    },
                                    Spans = libGroup.Select(x => x.Span).ToList()
                                })
                                .ToList()
                        })
                        .ToList()
                };

                output.Add((traceId, root));
            }

            return output;
        }
    }
}