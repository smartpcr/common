// -----------------------------------------------------------------------
// <copyright file="Matches.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions.Operators
{
    using System;
    using System.Linq.Expressions;
    using System.Text.RegularExpressions;

    public class Matches : OperatorExpression
    {
        private const string MethodName = "IsMatch";

        public Matches(Expression? leftExpression, Expression? rightExpression) : base(leftExpression, rightExpression)
        {
            if (leftExpression.Type != typeof(string) || rightExpression.Type != typeof(string))
            {
                throw new InvalidOperationException($"both left side and right side must be type string for method '{MethodName}'");
            }
        }

        public override Expression Create()
        {
            var regexOptionExpr = Expression.Constant(RegexOptions.IgnoreCase, typeof(RegexOptions));
            var regex = Expression.Call(
                typeof(Regex),
                MethodName,
                null,
                LeftExpression,
                RightExpression,
                regexOptionExpr);
            return regex;
        }
    }
}