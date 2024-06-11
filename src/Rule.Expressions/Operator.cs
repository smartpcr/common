// -----------------------------------------------------------------------
// <copyright file="Operator.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    [JsonConverter(typeof(StringEnumConverter))]
    public enum Operator
    {
        Equals,
        NotEquals,
        GreaterThan,
        GreaterOrEqual,
        LessThan,
        LessOrEqual,
        Contains,
        NotContains,
        ContainsAll,
        NotContainsAll,
        StartsWith,
        NotStartsWith,
        Matches,
        NotMatches,
        In,
        NotIn,
        AllIn,
        NotAllIn,
        AnyIn,
        NotAnyIn,
        IsNull,
        NotIsNull,
        IsEmpty,
        NotIsEmpty,

    }

    public static class OperatorExtension
    {
        public static bool IsMacro(this Operator @operator)
        {
            switch (@operator)
            {
                default:
                    return false;
            }
        }

        public static bool NullOrEmptyCheck(this Operator @operator)
        {
            switch (@operator)
            {
                case Operator.IsEmpty:
                case Operator.IsNull:
                case Operator.NotIsEmpty:
                case Operator.NotIsNull:
                    return true;
                default:
                    return false;
            }
        }
    }
}