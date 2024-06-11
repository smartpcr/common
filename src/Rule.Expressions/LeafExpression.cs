// -----------------------------------------------------------------------
// <copyright file="LeafExpression.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using Macros;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Operators;

    public class LeafExpression : IConditionExpression
    {
        [JsonProperty(Required = Required.Always)]
        public string Left { get; set; }

        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter), true)]
        public Operator Operator { get; set; }

        public string Right { get; set; }

        public bool RightSideIsExpression { get; set; }

        public string[] OperatorArgs { get; set; }

        public Expression Process(ParameterExpression ctxExpression, Type parameterType)
        {
            var leftExpression = ctxExpression.EvaluateExpression(Left, false);

            if (Operator != Operator.IsNull && Operator != Operator.NotIsNull)
            {
                leftExpression = leftExpression.AddValueWithNullableNumberType();
            }

            leftExpression = leftExpression.AddEnumToStringConvert();

            var leftSideType = leftExpression.Type;
            Expression? rightExpression;
            if (RightSideIsExpression)
            {
                rightExpression = ctxExpression.EvaluateExpression(Right);
                rightExpression = rightExpression.AddEnumToStringConvert().AddValueWithNullableNumberType();
            }
            else
            {
                rightExpression = GetRightConstantExpression(leftSideType);
            }

            Expression generatedExpression;
            switch (Operator)
            {
                case Operator.Equals:
                    generatedExpression = Expression.Equal(leftExpression, rightExpression);
                    break;
                case Operator.NotEquals:
                    generatedExpression = Expression.Not(Expression.Equal(leftExpression, rightExpression));
                    break;
                case Operator.GreaterThan:
                    generatedExpression = leftExpression.Type == typeof(DateTime)
                        ? Expression.MakeBinary(ExpressionType.GreaterThan, leftExpression, rightExpression)
                        : Expression.GreaterThan(leftExpression, rightExpression);
                    break;
                case Operator.GreaterOrEqual:
                    generatedExpression = leftExpression.Type == typeof(DateTime)
                        ? Expression.MakeBinary(ExpressionType.GreaterThanOrEqual, leftExpression, rightExpression)
                        : Expression.GreaterThanOrEqual(leftExpression, rightExpression);
                    break;
                case Operator.LessThan:
                    generatedExpression = leftExpression.Type == typeof(DateTime)
                        ? Expression.MakeBinary(ExpressionType.LessThan, leftExpression, rightExpression)
                        : Expression.LessThan(leftExpression, rightExpression);
                    break;
                case Operator.LessOrEqual:
                    generatedExpression = leftExpression.Type == typeof(DateTime)
                        ? Expression.MakeBinary(ExpressionType.LessThanOrEqual, leftExpression, rightExpression)
                        : Expression.LessThanOrEqual(leftExpression, rightExpression);
                    break;
                case Operator.Contains:
                    generatedExpression = new Contains(leftExpression, rightExpression).Create();
                    break;
                case Operator.NotContains:
                    generatedExpression = Expression.Not(new Contains(leftExpression, rightExpression).Create());
                    break;
                case Operator.ContainsAll:
                    generatedExpression = new ContainsAll(leftExpression, rightExpression).Create();
                    break;
                case Operator.NotContainsAll:
                    generatedExpression = Expression.Not(new ContainsAll(leftExpression, rightExpression).Create());
                    break;
                case Operator.StartsWith:
                    generatedExpression = new StartsWith(leftExpression, rightExpression).Create();
                    break;
                case Operator.NotStartsWith:
                    generatedExpression = Expression.Not(new StartsWith(leftExpression, rightExpression).Create());
                    break;
                case Operator.Matches:
                    generatedExpression = new Matches(leftExpression, rightExpression).Create();
                    break;
                case Operator.NotMatches:
                    generatedExpression = Expression.Not(new Matches(leftExpression, rightExpression).Create());
                    break;
                case Operator.In:
                    generatedExpression = new In(leftExpression, rightExpression).Create();
                    break;
                case Operator.NotIn:
                    generatedExpression = Expression.Not(new In(leftExpression, rightExpression).Create());
                    break;
                case Operator.AllIn:
                    generatedExpression = new AllIn(leftExpression, rightExpression).Create();
                    break;
                case Operator.NotAllIn:
                    generatedExpression = Expression.Not(new AllIn(leftExpression, rightExpression).Create());
                    break;
                case Operator.AnyIn:
                    generatedExpression = new AnyIn(leftExpression, rightExpression).Create();
                    break;
                case Operator.NotAnyIn:
                    generatedExpression = Expression.Not(new AnyIn(leftExpression, rightExpression).Create());
                    break;
                case Operator.IsNull:
                    if (leftSideType.IsPrimitiveType() && Nullable.GetUnderlyingType(leftSideType) != null)
                    {
                        rightExpression = Expression.Constant(null, leftSideType);
                    }

                    generatedExpression = Expression.Equal(leftExpression, rightExpression);
                    break;
                case Operator.NotIsNull:
                    if (leftSideType.IsPrimitiveType() && Nullable.GetUnderlyingType(leftSideType) != null)
                    {
                        rightExpression = Expression.Constant(null, leftSideType);
                    }

                    generatedExpression = Expression.Not(Expression.Equal(leftExpression, rightExpression));
                    break;
                case Operator.IsEmpty:
                    generatedExpression = new IsEmpty(leftExpression, rightExpression).Create();
                    break;
                case Operator.NotIsEmpty:
                    generatedExpression = Expression.Not(new IsEmpty(leftExpression, rightExpression).Create());
                    break;
                case Operator.AllInRangePct:
                    generatedExpression = new AllInRange(leftExpression, rightExpression, OperatorArgs).Create();
                    break;
                case Operator.ChannelNameEquals:
                case Operator.ChannelNameNotEquals:
                case Operator.ChannelNameContains:
                case Operator.ChannelNameNotContains:
                case Operator.ChannelNameStartsWith:
                case Operator.ChannelNameNotStartsWith:
                case Operator.QualityEquals:
                case Operator.QualityNotEquals:
                case Operator.InGoodQuality:
                case Operator.NotInMaintenance:
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
                    var methodName = Operator.ToString();
                    var extensionMethod = leftExpression.Type.GetExtensionMethods().First(m => m.Name == methodName);
                    if (extensionMethod == null)
                    {
                        throw new InvalidOperationException($"operator not mapped to extension method: {methodName}");
                    }

                    var macroExpression = new MacroExpressionCreator(leftExpression, extensionMethod, OperatorArgs).CreateMacroExpression();
                    generatedExpression = Expression.Equal(macroExpression, rightExpression);
                    break;
                default:
                    throw new NotSupportedException($"operation {Operator} is not supported");
            }

            if (Operator == Operator.IsNull || Operator == Operator.NotIsNull)
            {
                return generatedExpression;
            }

            return leftExpression.AddNotNullCheck(out var nullCheckExpression)
                ? Expression.AndAlso(nullCheckExpression!, generatedExpression)
                : generatedExpression;
        }

        public bool IsEmpty => false;

        private Expression? GetRightConstantExpression(Type leftSideType)
        {
            switch (Operator)
            {
                case Operator.ContainsAll:
                case Operator.NotContainsAll:
                case Operator.AllIn:
                case Operator.NotAllIn:
                case Operator.AnyIn:
                case Operator.NotAnyIn:
                    var stringArray = Right.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim()).ToArray();
                    return Expression.Constant(stringArray, typeof(string[]));
                case Operator.In:
                case Operator.NotIn:
                    var argumentValues = Right.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim()).ToArray();
                    return Expression.NewArrayInit(typeof(string), argumentValues.Select(Expression.Constant));
                case Operator.Contains:
                case Operator.NotContains:
                    return Expression.Constant(typeof(string).ConvertValue(Right));
                case Operator.IsNull:
                case Operator.NotIsNull:
                case Operator.IsEmpty:
                case Operator.NotIsEmpty:
                    return Expression.Constant(null, typeof(object));
                case Operator.AllInRangePct:
                    var decimalArray = Right.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim()).Select(decimal.Parse).ToArray();
                    var min = decimalArray.MinValue();
                    var max = decimalArray.MaxValue();
                    return Expression.Constant(new[] { min, max }, typeof(decimal[]));
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
                    if (!bool.TryParse(Right, out var boolValue))
                    {
                        boolValue = true;
                    }

                    return Expression.Constant(boolValue, typeof(bool));
                default:
                    return Expression.Constant(leftSideType.ConvertValue(Right));
            }
        }
    }
}