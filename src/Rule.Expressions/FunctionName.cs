// -----------------------------------------------------------------------
// <copyright file="FunctionName.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    [JsonConverter(typeof(StringEnumConverter))]
    public enum FunctionName
    {
        /// <summary>
        /// Count() on either scalar or complex obj
        /// </summary>
        Count,

        /// <summary>
        /// DistinctCount() on either scalar or complex obj
        /// </summary>
        DistinctCount,

        /// <summary>
        /// Average() or Average(propPath) on numeric values
        /// </summary>
        Average,

        /// <summary>
        /// Max() or Max(propPath) on numeric values
        /// </summary>
        Max,

        /// <summary>
        /// Min() or Min(propPath) on numeric values
        /// </summary>
        Min,

        /// <summary>
        /// Sum() or Sum(propPath) on numeric values
        /// </summary>
        Sum,

        /// <summary>
        /// only for datetime target, arg is positive int followed by one of ['m','h','d']
        /// such as: Ago(10m), Ago(1h), Ago(3d)
        /// </summary>
        Ago,

        /// <summary>
        /// Select(propPath)
        /// </summary>
        Select,

        /// <summary>
        /// SelectMany(propPath)
        /// </summary>
        SelectMany,

        /// <summary>
        /// Where(fieldName, operator, fieldValue)
        /// fieldName is prop name
        /// operator can only be binary: equals, notEquals, greaterThan, greaterOrEqual, lessThan, lessOrEqual
        /// fieldValue must be constant
        /// </summary>
        Where,

        /// <summary>
        /// First(fieldName, operator, fieldValue)
        /// fieldName is prop name
        /// operator can only be binary: equals, notEquals, greaterThan, greaterOrEqual, lessThan, lessOrEqual
        /// fieldValue must be constant
        /// </summary>
        First,

        Last,

        OrderBy,

        OrderByDesc,
    }
}