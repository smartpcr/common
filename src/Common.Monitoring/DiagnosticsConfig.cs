// -----------------------------------------------------------------------
// <copyright file="DiagnosticsConfig.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring
{
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using Microsoft.Extensions.AmbientMetadata;

    public class DiagnosticsConfig
    {
        public ActivitySource ActivitySource { get; private set; }
        public Meter Meter { get; private set; }

        public DiagnosticsConfig(ApplicationMetadata metadata)
        {
            this.ActivitySource = new ActivitySource(metadata.ApplicationName);
            this.Meter = new Meter(metadata.ApplicationName);
        }
    }
}