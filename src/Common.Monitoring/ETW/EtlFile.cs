// -----------------------------------------------------------------------
// <copyright file="EtlFile.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.ETW
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using Microsoft.Diagnostics.Tracing;
    using Microsoft.Diagnostics.Tracing.Parsers;

    public class EtlFile
    {
        private readonly string etlFile;
        private readonly long fileSize;

        public EtlFile(string etlFile)
        {
            this.etlFile = etlFile;
            this.fileSize = new FileInfo(etlFile).Length;
        }

        public void Parse(ConcurrentDictionary<(string providerName, string eventName), EtwEvent> eventSchema, ref bool failed)
        {
            using var source = new ETWTraceEventSource(this.etlFile);
            var parser = new DynamicTraceEventParser(source);

            var stopwatch = Stopwatch.StartNew();
            var lastEventTime = DateTime.UtcNow;
            var timer = new System.Timers.Timer(10000); // 10 seconds
            timer.Elapsed += (_, _) =>
            {
                if ((DateTime.UtcNow - lastEventTime).TotalSeconds >= 10)
                {
                    Console.WriteLine($"No events received in the last 10 seconds. Stopping processing {this.etlFile}  (file size: {this.fileSize} bytes) after {Math.Round(stopwatch.Elapsed.TotalSeconds)} seconds.");
                    source.StopProcessing();
                    timer.Stop();
                }
            };
            timer.Start();

            parser.All += traceEvent =>
            {
                try
                {
                    lastEventTime = DateTime.UtcNow; // Update the last event time
                    var providerName = traceEvent.ProviderName;
                    var eventName = traceEvent.EventName;
                    var key = (providerName, eventName);

                    // Use TryGetValue and TryAdd to minimize lookups
                    if (!eventSchema.TryGetValue(key, out var etwEvent))
                    {
                        etwEvent = new EtwEvent
                        {
                            ProviderName = providerName,
                            EventName = eventName,
                            PayloadSchema = new List<(string fieldName, Type fieldType)>(),
                            Payload = new Dictionary<string, object>(),
                        };

                        etwEvent.PayloadSchema.Add((nameof(traceEvent.TimeStamp), typeof(DateTime)));
                        etwEvent.PayloadSchema.Add((nameof(traceEvent.ProcessID), typeof(int)));
                        etwEvent.PayloadSchema.Add((nameof(traceEvent.ProcessName), typeof(string)));
                        etwEvent.PayloadSchema.Add((nameof(traceEvent.Level), typeof(int)));
                        etwEvent.PayloadSchema.Add((nameof(traceEvent.Opcode), typeof(string)));
                        etwEvent.PayloadSchema.Add((nameof(traceEvent.OpcodeName), typeof(string)));

                        etwEvent.Payload.Add(nameof(traceEvent.TimeStamp), traceEvent.TimeStamp);
                        etwEvent.Payload.Add(nameof(traceEvent.ProcessID), traceEvent.ProcessID);
                        etwEvent.Payload.Add(nameof(traceEvent.ProcessName), traceEvent.ProcessName);
                        etwEvent.Payload.Add(nameof(traceEvent.Level), traceEvent.Level);
                        etwEvent.Payload.Add(nameof(traceEvent.Opcode), traceEvent.Opcode);
                        etwEvent.Payload.Add(nameof(traceEvent.OpcodeName), traceEvent.OpcodeName);

                        foreach (var item in traceEvent.PayloadNames)
                        {
                            if (etwEvent.Payload.TryAdd(item, traceEvent.PayloadByName(item)))
                            {
                                etwEvent.PayloadSchema.Add((item, traceEvent.PayloadByName(item)?.GetType() ?? typeof(string)));
                            }
                        }

                        eventSchema.TryAdd(key, etwEvent);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing event: {ex.Message}");
                    source.StopProcessing();
                }
            };

            try
            {
                source.Process();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing ETL file: {ex.Message}");
                failed = true;
            }
            finally
            {
                timer.Stop();
                timer.Dispose();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        public Dictionary<(string providerName, string eventName), List<string>> Process(
            Dictionary<(string providerName, string eventName), EtwEvent> eventSchemas)
        {
            using var source = new ETWTraceEventSource(this.etlFile);
            var parser = new DynamicTraceEventParser(source);

            var stopwatch = Stopwatch.StartNew();
            var lastEventTime = DateTime.UtcNow;
            var timer = new System.Timers.Timer(10000); // 10 seconds
            timer.Elapsed += (_, _) =>
            {
                if ((DateTime.UtcNow - lastEventTime).TotalSeconds >= 10)
                {
                    Console.WriteLine($"No events received in the last 10 seconds. Stopping processing {this.etlFile}  (file size: {this.fileSize} bytes) after {Math.Round(stopwatch.Elapsed.TotalSeconds)} seconds.");
                    source.StopProcessing();
                    timer.Stop();
                }
            };
            timer.Start();

            var fileContentsByProviderEvents = new Dictionary<(string providerName, string eventName), List<string>>();
            parser.All += traceEvent =>
            {
                lastEventTime = DateTime.UtcNow;

                var providerName = traceEvent.ProviderName;
                var eventName = traceEvent.EventName;
                if (!fileContentsByProviderEvents.TryGetValue((providerName, eventName), out var csvLines))
                {
                    csvLines = new List<string>();
                    fileContentsByProviderEvents.Add((providerName, eventName), csvLines);
                }

                if (eventSchemas.TryGetValue((providerName, eventName), out var eventSchema))
                {
                    var rowBuilder = new StringBuilder();
                    for (var i = 0; i < eventSchema.PayloadSchema.Count; i++)
                    {
                        var (fieldName, fieldType) = eventSchema.PayloadSchema[i];
                        switch (fieldName)
                        {
                            case nameof(traceEvent.TimeStamp):
                                rowBuilder.Append(traceEvent.TimeStamp);
                                break;
                            case nameof(traceEvent.ProcessID):
                                rowBuilder.Append(traceEvent.ProcessID);
                                break;
                            case nameof(traceEvent.ProcessName):
                                rowBuilder.Append(traceEvent.ProcessName);
                                break;
                            case nameof(traceEvent.Level):
                                rowBuilder.Append(traceEvent.Level);
                                break;
                            case nameof(traceEvent.Opcode):
                                rowBuilder.Append(traceEvent.Opcode);
                                break;
                            case nameof(traceEvent.OpcodeName):
                                rowBuilder.Append(traceEvent.OpcodeName);
                                break;
                            default:
                                if (fieldType == typeof(string) && traceEvent.PayloadByName(fieldName) is string fieldValue)
                                {
                                    var containsSpecialCharacters =
                                        fieldValue.Contains('"') ||
                                        fieldValue.Contains(',') ||
                                        fieldValue.Contains(' ') ||
                                        fieldValue.Contains('\n') ||
                                        fieldValue.Contains('\r');
                                    if (containsSpecialCharacters)
                                    {
                                        // Escape quotes by doubling them
                                        var escapedField = fieldValue.Replace("\"", "\"\"");

                                        // Wrap the field in quotes
                                        rowBuilder.Append($"\"{escapedField}\"");
                                    }
                                }
                                else
                                {
                                    rowBuilder.Append(traceEvent.PayloadByName(fieldName));
                                }

                                break;
                        }

                        if (i < eventSchema.PayloadSchema.Count - 1)
                        {
                            rowBuilder.Append(",");
                        }
                    }

                    csvLines.Add(rowBuilder.ToString());
                }
            };

            source.Process();

            return fileContentsByProviderEvents;
        }
    }
}