// -----------------------------------------------------------------------
// <copyright file="EnrichmentSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Enrichment;

using Microsoft.Extensions.AmbientMetadata;
using Microsoft.R9.Extensions.Enrichment;

public class EnrichmentSettings
{
    public ApplicationMetadata Metadata { get; set; }
    public ServiceTraceEnricherOptions EnricherOptions { get; set; }
}