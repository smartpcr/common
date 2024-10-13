// -----------------------------------------------------------------------
// <copyright file="EvtxRecord.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.ETW
{
    using System;

    public class EvtxRecord
    {
        public DateTimeOffset TimeStamp { get; set; }
        public string ProviderName { get; set; }
        public string LogName { get; set; }
        public string MachineName { get; set; }
        public int EventId { get; set; }
        public string Level { get; set; }

        public short? Opcode { get; set; }
        public string Keywords { get; set; }
        public int? ProcessId { get; set; }
        public string Description { get; set; }
    }
}