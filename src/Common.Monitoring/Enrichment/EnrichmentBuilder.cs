// -----------------------------------------------------------------------
// <copyright file="EnrichmentBuilder.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Enrichment;

using Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.R9.Extensions.Enrichment;
using OpenTelemetry.Trace;

public static class EnrichmentBuilder
{
    private const string EnrichmentMetadataSectionName = "EnrichmentSettings:Metadata";
    private const string EnricherOptionsSectionName = "EnrichmentSettings:EnricherOptions";
    private const string AmbientMetadataSectionName = "ambientmetadata:application";
    private const string AmbientMetadataFallbackSectionName = "R9:ambientmetadata:application";

    public static IServiceCollection AddEnrichment(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplicationMetadata(configuration.GetSection(AmbientMetadataSectionName))
            .AddApplicationMetadata(configuration.GetSection(AmbientMetadataFallbackSectionName))
            .AddApplicationMetadata(configuration.GetSection(EnrichmentMetadataSectionName))
            .AddServiceLogEnricher()
            .AddCorrelationVectorLogEnricher()
            .AddProcessLogEnricher();

        var monitorSettings = configuration.GetConfiguredSettings<MonitorSettings>();
        if (monitorSettings.UseOpenTelemetry())
        {
            services.AddOpenTelemetry()
                .WithTracing(builder =>
                {
                    builder.AddTracerEnrichment(configuration);
                });
        }

        return services;
    }

    private static void AddTracerEnrichment(this TracerProviderBuilder tracerBuilder, IConfiguration configuration)
    {
        tracerBuilder.AddSource(nameof(EnrichmentBuilder))
            .AddServiceTraceEnricher(configuration.GetSection(EnricherOptionsSectionName))
            .AddCorrelationVectorTraceEnricher()
            .AddConsoleExporter();
    }
}