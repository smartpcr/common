// -----------------------------------------------------------------------
// <copyright file="EtlFile.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Kusto.Tests.Steps
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Diagnostics.Tracing;
    using Microsoft.Diagnostics.Tracing.Etlx;
    using Microsoft.Diagnostics.Tracing.Parsers;

    public class EtlFile
    {
        private readonly string etlFile;

        public EtlFile(string etlFile)
        {
            this.etlFile = etlFile;
        }

        public Dictionary<(string providerName, string eventName), EtwEvent> Parse()
        {
            var eventSchema = new Dictionary<(string providerName, string eventName), EtwEvent>();
            using var source = new ETWTraceEventSource(this.etlFile);
            var parser = new DynamicTraceEventParser(source);
            parser.All += traceEvent =>
            {
                var providerName = traceEvent.ProviderName;
                var eventName = traceEvent.EventName;
                if (!eventSchema.ContainsKey((providerName, eventName)))
                {
                    var etwEvent = new EtwEvent
                    {
                        ProviderName = providerName,
                        EventName = eventName,
                        PayloadSchema = new Dictionary<string, Type>(),
                        Payload = new Dictionary<string, object>(),
                    };
                    foreach (var item in traceEvent.PayloadNames)
                    {
                        etwEvent.PayloadSchema.Add(item, traceEvent.PayloadByName(item).GetType());
                        etwEvent.Payload.Add(item, traceEvent.PayloadByName(item));
                    }
                    eventSchema.Add((providerName, eventName), etwEvent);
                }

            };

            source.Process();

            return eventSchema;
        }
    }
}