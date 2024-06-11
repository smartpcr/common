// -----------------------------------------------------------------------
// <copyright file="Where.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions.Functions
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text.RegularExpressions;
    using Operators;

    public class Where : FunctionExpression
    {
        private readonly string fieldName;
        private readonly Operator op;
        private readonly string fieldValue;
        private readonly Expression? valueExpression;
        private readonly Type argType;

        public Where(Expression target, params string[] args) : base(target, FunctionName.Where, args)
        {
            if (args == null || args.Length != 3)
            {
                throw new ArgumentException($"exactly 3 args required for function '{FunctionName.Where}'");
            }

            fieldName = args[0];
            op = (Operator) Enum.Parse(typeof(Operator), args[1], true);
            fieldValue = args[2];
            fieldValue = fieldValue.Trim('\'', '"').Trim();
            if (IsValueStaticFunction(fieldValue, out var valueFunc))
            {
                valueExpression = valueFunc!;
            }

            if (Target.Type.IsGenericType)
            {
                argType = Target.Type.GetGenericArguments()[0];
            }
            else if (Target.Type.IsArray)
            {
                argType = Target.Type.GetElementType()!;
            }
            else
            {
                throw new InvalidOperationException($"target expression type '{Target.Type.Name}' is invalid, it should be either an array or enumerable");
            }
        }

        public override Expression Build()
        {
            var argParameter = Expression.Parameter(argType, "s");
            var propNames = fieldName.Split(new[] {'.'});
            Expression? propExpression = argParameter;
            foreach (var propName in propNames)
            {
                var prop = propExpression.Type.GetMappedProperty(propName);
                propExpression = Expression.Property(propExpression, prop);
            }
            Expression? valueExpr = this.valueExpression;
            if (valueExpr.Type != propExpression.Type)
            {
                if ((valueExpr.Type == typeof(string) && propExpression.Type == typeof(int)) || propExpression.Type == typeof(int?))
                {
                    valueExpr = Expression.Convert(Expression.Constant(int.Parse(fieldValue)), propExpression.Type);
                }
                else if ((valueExpr.Type == typeof(string) && propExpression.Type == typeof(decimal)) || propExpression.Type == typeof(decimal?))
                {
                    valueExpr = Expression.Convert(Expression.Constant(decimal.Parse(fieldValue)), propExpression.Type);
                }
                else if ((valueExpr.Type == typeof(string) && propExpression.Type == typeof(double)) || propExpression.Type == typeof(double?))
                {
                    valueExpr = Expression.Convert(Expression.Constant(double.Parse(fieldValue)), propExpression.Type);
                }
                else if ((valueExpr.Type == typeof(string) && propExpression.Type == typeof(bool)) || propExpression.Type == typeof(bool?))
                {
                    valueExpr = Expression.Convert(Expression.Constant(bool.Parse(fieldValue)), propExpression.Type);
                }
                else
                {
                    valueExpr = Expression.Convert(valueExpr, propExpression.Type);
                }
            }

            Expression predicate;
            switch (op)
            {
                case Operator.Equals:
                    predicate = Expression.Equal(propExpression, valueExpr);
                    break;
                case Operator.NotEquals:
                    predicate = Expression.NotEqual(propExpression, valueExpr);
                    break;
                case Operator.GreaterThan:
                    predicate = Expression.GreaterThan(propExpression, valueExpr);
                    break;
                case Operator.GreaterOrEqual:
                    predicate = Expression.GreaterThanOrEqual(propExpression, valueExpr);
                    break;
                case Operator.LessThan:
                    predicate = Expression.LessThan(propExpression, valueExpr);
                    break;
                case Operator.LessOrEqual:
                    predicate = Expression.LessThanOrEqual(propExpression, valueExpr);
                    break;
                case Operator.Contains:
                    predicate = new Contains(propExpression, valueExpr).Create();
                    break;
                default:
                    throw new NotSupportedException($"operator {op} is not supported in function '{FuncName}'");
            }

            var predicateExpr = Expression.Lambda(predicate, argParameter);
            return Expression.Call(
                typeof(Enumerable),
                "Where",
                new[] {argType},
                Target,
                predicateExpr);
        }

        private bool IsValueStaticFunction(string value, out Expression? valueFunction)
        {
            valueFunction = null;
            var functionRegexPatterns = FunctionNameExtension.GetFunctionNameRegexPatterns();
            foreach (var pattern in functionRegexPatterns)
            {
                var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                var match = regex.Match(value);
                if (match.Success)
                {
                    var functionName = match.Groups[1].Value;
                    var functionArg = match.Groups[2].Value;
                    var args = functionArg.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()).ToArray();
                    var funcName = (FunctionName)Enum.Parse(typeof(FunctionName), functionName, true);
                    var funcExpr = new FunctionExpressionCreator().Create(null, funcName, args);
                    valueFunction = funcExpr.Build();
                    return true;
                }
            }

            return false;
        }
    }
}