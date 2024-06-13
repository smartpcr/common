// -----------------------------------------------------------------------
// <copyright file="StartsWith.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions.Operators
{
    using System;
    using System.Linq.Expressions;

    public class StartsWith : OperatorExpression
    {
        private const string MethodName = "StartsWith";

        public StartsWith(Expression? leftExpression, Expression? rightExpression) : base(leftExpression, rightExpression)
        {
            if (leftExpression.Type != typeof(string) || rightExpression.Type != typeof(string))
            {
                throw new InvalidOperationException($"both left side and right side must be type string for method '{MethodName}'");
            }
        }

        public override Expression Create()
        {
            var methodInfo = typeof(string).GetMethod(MethodName, new[] {typeof(string)});
            if (methodInfo == null) throw new Exception("Invalid method: " + MethodName + " for type string");
            return Expression.Call(LeftExpression, methodInfo, RightExpression);
        }
    }
}