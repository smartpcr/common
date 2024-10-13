// -----------------------------------------------------------------------
// <copyright file="EvtxFileParser.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.ETW
{
    using System.Collections.Generic;
    using System.IO;
    using evtx;

    public class EvtxFileParser
    {
        private readonly string evtxFile;

        public EvtxFileParser(string evtxFile)
        {
            this.evtxFile = evtxFile;
        }

        public List<EvtxRecord> Parse()
        {
            var records = new List<EvtxRecord>();
            using var fs = new FileStream(this.evtxFile, FileMode.Open, FileAccess.Read);
            var es = new EventLog(fs);

            foreach (var record in es.GetEventRecords())
            {
                records.Add(new EvtxRecord()
                {
                    TimeStamp = record.TimeCreated,
                    ProviderName = record.Provider,
                    LogName = record.Channel,
                    MachineName = record.Computer,
                    EventId = record.EventId,
                    Level = record.Level,
                    Keywords = record.Keywords,
                    ProcessId = record.ProcessId,
                    Description = record.MapDescription,
                });
            }

            return records;
        }
    }
}