// -----------------------------------------------------------------------
// <copyright file="TraceFileParser.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Tracing
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using Newtonsoft.Json;

    public class SimpleSpan
    {
        public string Id { get; set; }

        [JsonProperty("StartTimeUtc")]
        public DateTimeOffset Timestamp { get; set; }

        [JsonProperty("RootId")]
        public string TraceId { get; set; }

        public string? ParentId { get; set; }

        [JsonIgnore]
        public SimpleSpan? Parent { get; set; }

        [JsonIgnore]
        public string ParentOperationName => this.Parent?.OperationName ?? string.Empty;

        public string OperationName { get; set; }

        [JsonIgnore]
        public string SourceName => this.Source.Name;

        public SimpleSpanSource Source { get; set; }

        [JsonIgnore]
        public Dictionary<string, object?> Attributes { get; set; }

        public List<KeyValuePair<string, object?>> TagObjects { get; set; }

        public List<KeyValuePair<string, string?>> Tags { get; set; }

        public SimpleSpan()
        {
            this.Attributes = new Dictionary<string, object?>();
        }
    }

    public class SimpleSpanSource
    {
        public string Name { get; set; }
        public string Version { get; set; }
    }

    public class TraceFileParser
    {
        private readonly string traceFilePath;

        public TraceFileParser(string traceFilePath)
        {
            this.traceFilePath = traceFilePath;
        }

        public List<SimpleSpan> Parse(string sourceName)
        {
            var output = new List<SimpleSpan>();
            var traceLines = File.ReadAllLines(this.traceFilePath);
            var traceIdRegex = new Regex(@"^(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d+Z) Id: ([0-9\-a-f]+), Trace: $");

            for (var i = 0; i < traceLines.Length; i++)
            {
                var match = traceIdRegex.Match(traceLines[i]);
                if (match.Success && i + 1 < traceLines.Length)
                {
                    var jsonTrace = traceLines[++i].Trim();
                    try
                    {
                        if (!string.IsNullOrEmpty(jsonTrace))
                        {
                            var span = JsonConvert.DeserializeObject<SimpleSpan>(jsonTrace);
                            if (span != null && span.SourceName == sourceName)
                            {
                                foreach (var tag in span.Tags)
                                {
                                    span.Attributes[tag.Key] = tag.Value;
                                }

                                foreach (var tag in span.TagObjects)
                                {
                                    span.Attributes[tag.Key] = tag.Value;
                                }

                                output.Add(span);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deserializing JSON: {ex.Message}");
                    }
                }
            }

            // select parent
            foreach (var span in output)
            {
                if (span.ParentId != null)
                {
                    span.Parent = output.Find(s => s.Id == span.ParentId);
                }
            }

            return output;
        }
    }
}