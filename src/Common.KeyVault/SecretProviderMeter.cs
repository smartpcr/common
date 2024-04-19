// -----------------------------------------------------------------------
// <copyright file="SecretProviderMeter.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.KeyVault;

using Microsoft.R9.Extensions.Metering;

internal static partial class SecretProviderMeter
{
    [Counter(typeof(SecretProviderDimension))]
    public static partial TotalSecretFailures CreateTotalSecretFailures(IMeter meter);

    [Histogram]
    public static partial GetSecretDuration CreateGetSecretDuration(IMeter meter);

    [Counter(typeof(SecretProviderDimension))]
    public static partial TotalCertFailures CreateTotalCertFailures(IMeter meter);

    [Histogram]
    public static partial GetCertDuration CreateGetCertDuration(IMeter meter);
}