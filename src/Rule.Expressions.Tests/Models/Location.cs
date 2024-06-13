// -----------------------------------------------------------------------
// <copyright file="Location.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions.Tests.Models
{
    public class Location
    {
        public string Country { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public string Street { get; set; }
        public string ZipCode { get; set; }
        public int Population { get; set; }
        public double? AvgIncome { get; set; }
    }
}