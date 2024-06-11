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
        AllInRangePct,
        ChannelNameEquals,
        ChannelNameNotEquals,
        ChannelNameContains,
        ChannelNameNotContains,
        ChannelNameStartsWith,
        ChannelNameNotStartsWith,
        QualityEquals,
        QualityNotEquals,
        InGoodQuality,
        NotInMaintenance,
        DataPointValueGreaterThanRatingPct,
        IsNotStale,
        CheckStaleness,
        StaledAmpsChannels,
        StaledS1AmpsChannels,
        AllAmpsChannelsAreNotStale,
        AllS1AmpsChannelsAreNotStale,
        StaledVoltChannels,
        StaledS1VoltChannels,
        AllVoltChannelsAreNotStale,
        AllS1VoltChannelsAreNotStale,
        MaxChannelVoltGreaterThanRating,
        MaxS1ChannelVoltGreaterThanRating,
        AtOrBelowHierarchy,
        BelowHierarchy,
        AtOrAboveHierarchy,
        AboveHierarchy
    }

    public static class OperatorExtension
    {
        public static bool IsMacro(this Operator @operator)
        {
            switch (@operator)
            {
                case Operator.ChannelNameEquals:
                case Operator.ChannelNameNotEquals:
                case Operator.ChannelNameContains:
                case Operator.ChannelNameNotContains:
                case Operator.ChannelNameStartsWith:
                case Operator.ChannelNameNotStartsWith:
                case Operator.QualityEquals:
                case Operator.QualityNotEquals:
                case Operator.DataPointValueGreaterThanRatingPct:
                case Operator.IsNotStale:
                case Operator.CheckStaleness:
                case Operator.StaledAmpsChannels:
                case Operator.StaledS1AmpsChannels:
                case Operator.AllAmpsChannelsAreNotStale:
                case Operator.AllS1AmpsChannelsAreNotStale:
                case Operator.StaledVoltChannels:
                case Operator.StaledS1VoltChannels:
                case Operator.AllVoltChannelsAreNotStale:
                case Operator.AllS1VoltChannelsAreNotStale:
                case Operator.MaxChannelVoltGreaterThanRating:
                case Operator.MaxS1ChannelVoltGreaterThanRating:
                case Operator.AtOrBelowHierarchy:
                case Operator.BelowHierarchy:
                case Operator.AtOrAboveHierarchy:
                case Operator.AboveHierarchy:
                    return true;
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