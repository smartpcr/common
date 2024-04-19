// -----------------------------------------------------------------------
// <copyright file="ApiRequestMetric.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Tests.Steps;

using Microsoft.R9.Extensions.Metering;

internal static partial class ApiRequestMetric
{
    [Counter]
    public static partial TotalRequests CreateTotalRequests(IMeter meter);

    [Counter]
    public static partial SuccessfulRequests CreateSuccessfulRequests(IMeter meter);

    [Counter]
    public static partial FailedRequests CreateFailedRequests(IMeter meter);

    [Histogram]
    public static partial RequestLatency CreateRequestLatency(IMeter meter);
}