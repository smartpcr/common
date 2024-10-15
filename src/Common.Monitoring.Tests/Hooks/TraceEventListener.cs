// -----------------------------------------------------------------------
// <copyright file="TraceEventListener.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Tests.Hooks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using System.Threading;
    using Common.Config.Tests.Hooks;
    using Microsoft.Diagnostics.Tracing.Session;
    using Newtonsoft.Json;
    using TechTalk.SpecFlow;
    using TechTalk.SpecFlow.Infrastructure;

    [Binding]
    public class TraceEventListener : IDisposable
    {
        private readonly ScenarioContext context;
        private readonly ISpecFlowOutputHelper outputHelper;
        private readonly TraceEventSession session;
        private Thread thread;
        private readonly List<TestTraceEvent> traceEvents = new List<TestTraceEvent>();

        public TraceEventListener(ScenarioContext context, ISpecFlowOutputHelper outputHelper, List<string> providerNames)
        {
            this.context = context;
            this.outputHelper = outputHelper;

            this.session = new TraceEventSession($"{nameof(TraceEventListener)}-{context.ScenarioInfo.Title}");
            foreach (var providerName in providerNames)
            {
                this.session.EnableProvider(providerName);
            }

            this.context.Set(this.traceEvents);
        }

        public void Start()
        {
            this.session.Source.Dynamic.All += (data) =>
            {
                try
                {
                    var payloadJson = data.PayloadNames.Length > 0
                        ? JsonConvert.SerializeObject(data.PayloadNames.ToDictionary(name => name, name => data.PayloadByName(name)))
                        : string.Empty;
                    this.outputHelper.WriteVerbose($"{data.TimeStamp:MM/dd/yyyy hh:mm:ss.fffff}: [{data.ProviderName}/{data.EventName}({data.ID})] - {payloadJson}");
                    this.traceEvents.Add(new TestTraceEvent()
                    {
                        ProviderName = data.ProviderName,
                        EventName = data.EventName,
                        Timestamp = data.TimeStamp,
                        Payload = data.PayloadNames.ToDictionary(name => name, name => data.PayloadByName(name)?.ToString())
                    });
                }
                catch (EventSourceException ex)
                {
                    this.outputHelper.WriteError($"EventSourceException while processing event \"{data.EventName}\": {ex.Message}");
                }
            };
            this.thread = new Thread(() => this.session.Source.Process());
            this.thread.Start();
        }

        public void Dispose()
        {
            this.session.Stop();
            this.thread.Join();
            this.session.Dispose();
        }
    }

    public class TestTraceEvent
    {
        public string ProviderName { get; set; }
        public string EventName { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public Dictionary<string, string?> Payload { get; set; }
    }
}