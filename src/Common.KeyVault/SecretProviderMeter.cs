// -----------------------------------------------------------------------
// <copyright file="SecretProviderMeter.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.KeyVault;

using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.AmbientMetadata;

internal class SecretProviderMeter
{
    private readonly Counter<long> _totalSecretFailures;
    private readonly Histogram<double> _getSecretDuration;
    private readonly Counter<long> _totalCertFailures;
    private readonly Histogram<double> _getCertDuration;

    private SecretProviderMeter(ApplicationMetadata metadata)
    {
        var meter = new Meter($"{metadata.ApplicationName}.{nameof(SecretProviderMeter)}");
        this._totalSecretFailures = meter.CreateCounter<long>("TotalSecretFailures", "Total number of secret failures");
        this._getSecretDuration = meter.CreateHistogram<double>("GetSecretDuration", "Get secret duration in milliseconds");
        this._totalCertFailures = meter.CreateCounter<long>("TotalCertFailures", "Total number of cert failures");
        this._getCertDuration = meter.CreateHistogram<double>("GetCertDuration", "Get cert duration in milliseconds");
    }

    public static SecretProviderMeter Instance(ApplicationMetadata metadata)
    {
        return new SecretProviderMeter(metadata);
    }

    public void IncrementTotalSecretFailures(params KeyValuePair<string, object?>[] dimensions)
    {
        this._totalSecretFailures.Add(1, dimensions);
    }

    public void RecordGetSecretDuration(double duration, params KeyValuePair<string, object?>[] dimensions)
    {
        this._getSecretDuration.Record(duration, dimensions);
    }

    public void IncrementTotalCertFailures(params KeyValuePair<string, object?>[] dimensions)
    {
        this._totalCertFailures.Add(1, dimensions);
    }

    public void RecordGetCertDuration(double duration, params KeyValuePair<string, object?>[] dimensions)
    {
        this._getCertDuration.Record(duration, dimensions);
    }
}