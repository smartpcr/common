// -----------------------------------------------------------------------
// <copyright file="FunctionNameExtension.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class FunctionNameExtension
    {
        public static List<string> GetAllFunctionNames()
        {
            return (from object e in Enum.GetValues(typeof(FunctionName)) select e.ToString()).ToList();
        }

        public static List<string> GetFunctionNameRegexPatterns()
        {
            var functionNames = GetAllFunctionNames();
            return functionNames.Select(f => $@"^({f})\((.*)\)$").ToList();
        }

        public static bool IsAggregateFunction(this FunctionName functionName)
        {
            switch (functionName)
            {
                case FunctionName.Average:
                case FunctionName.Count:
                case FunctionName.DistinctCount:
                case FunctionName.Max:
                case FunctionName.Min:
                case FunctionName.Sum:
                case FunctionName.First:
                case FunctionName.Last:
                    return true;
                default:
                    return false;
            }
        }

        public static bool ReturnTypeIsInt(this FunctionName functionName)
        {
            return functionName == FunctionName.Count || functionName == FunctionName.DistinctCount;
        }

        public static bool AllowMemberAggregate(this FunctionName functionName)
        {
            switch (functionName)
            {
                case FunctionName.Average:
                case FunctionName.Max:
                case FunctionName.Min:
                case FunctionName.Sum:
                case FunctionName.First:
                case FunctionName.Last:
                    return true;
                default:
                    return false;
            }
        }
    }
}