// -----------------------------------------------------------------------
// <copyright file="EnumerableExtension.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class EnumerableExtension
    {
        public static TResult? MaxValue<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selectMethod, TResult? defaultOutput = default)
        {
            var sourceArray = source.ToList();
            return sourceArray.Any() ? sourceArray.Max(selectMethod) : defaultOutput;
        }

        public static TResult? MaxValue<TResult>(this IEnumerable<TResult> source, TResult? defaultOutput = default)
        {
            var sourceArray = source.ToList();
            return sourceArray.Any() ? sourceArray.Max() : defaultOutput;
        }

        public static TResult? MinValue<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selectMethod, TResult? defaultOutput = default)
        {
            var sourceArray = source.ToList();
            return sourceArray.Any() ? sourceArray.Min(selectMethod) : defaultOutput;
        }

        public static TResult? MinValue<TResult>(this IEnumerable<TResult> source, TResult? defaultOutput = default)
        {
            var sourceArray = source.ToList();
            return sourceArray.Any() ? sourceArray.Min() : defaultOutput;
        }

        public static double AvgValue<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selectMethod, double defaultOutput = default)
        {
            var sourceArray = source.ToList();
            return sourceArray.Any() ? sourceArray.Average(selectMethod) : defaultOutput;
        }

        public static double AvgValue(this IEnumerable<double> source, double defaultOutput = default)
        {
            var sourceArray = source.ToList();
            return sourceArray.Any() ? sourceArray.Average() : defaultOutput;
        }
    }
}