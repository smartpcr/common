// -----------------------------------------------------------------------
// <copyright file="Meter_T.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Metrics
{
    using System.Diagnostics.Metrics;

    public class Meter<T> : Meter
    {
        public Meter()
            : base(typeof(T).FullName!)
        {
        }
    }
}