// -----------------------------------------------------------------------
// <copyright file="AllInRange.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions.Operators
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;

    public class AllInRange : OperatorExpression
    {
        private readonly decimal threshold;
        private readonly Type argType;

        public AllInRange(Expression? leftExpression, Expression? rightExpression, params string[] operatorArgs)
            : base(leftExpression, rightExpression)
        {
            if (!leftExpression.Type.IsGenericType && !leftExpression.Type.IsArray)
            {
                throw new InvalidOperationException($"only array or enumerable is allowed for left side expression");
            }

            if (leftExpression.Type.IsGenericType)
            {
                argType = leftExpression.Type.GetGenericArguments()[0];
            }
            else if (leftExpression.Type.IsArray)
            {
                argType = leftExpression.Type.GetElementType()!;
            }
            if (argType == null || !argType.IsNumericType())
            {
                throw new InvalidOperationException("only numeric item type is allowed on left side expression");
            }

            if (rightExpression.Type != typeof(decimal[]))
            {
                throw new InvalidOperationException("only decimal array is allowed for right side expression");
            }

            if (operatorArgs == null || operatorArgs.Length != 1)
            {
                throw new InvalidOperationException($"Operator {GetType().Name} requires one argument");
            }

            threshold = decimal.Parse(operatorArgs[0]);
        }

        public override Expression Create()
        {
            var thresholdMin = Expression.Constant((100-threshold)/100, typeof(decimal));
            var thresholdMax = Expression.Constant((100+threshold)/100, typeof(decimal));
            var min = Expression.ArrayIndex(RightExpression, Expression.Constant(0));
            min = Expression.Multiply(min, thresholdMin);
            var max = Expression.ArrayIndex(RightExpression, Expression.Constant(1));
            max = Expression.Multiply(max, thresholdMax);

            var numParamExpr = Expression.Parameter(argType, "n");
            var compareBody = Expression.AndAlso(
                Expression.LessThanOrEqual(Expression.Convert(numParamExpr, typeof(decimal)), max),
                Expression.GreaterThanOrEqual(Expression.Convert(numParamExpr, typeof(decimal)), min));
            var predicateExpr = Expression.Lambda(compareBody, numParamExpr);

            var allInExpression = Expression.Call(
                typeof(Enumerable),
                "All",
                new[] { argType },
                LeftExpression,
                predicateExpr);

            return allInExpression;
        }
    }
}