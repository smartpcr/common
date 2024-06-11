// -----------------------------------------------------------------------
// <copyright file="IsEmpty.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions.Operators
{
    using System.Linq;
    using System.Linq.Expressions;

    public class IsEmpty : OperatorExpression
    {
        public IsEmpty(Expression? leftExpression, Expression? rightExpression) : base(leftExpression, rightExpression)
        {
        }

        public override Expression Create()
        {
            var isNull = Expression.Equal(LeftExpression, Expression.Constant(null, LeftExpression.Type));
            var anyCheck = Expression.Call(
                typeof(Enumerable),
                "Any",
                LeftExpression.Type.IsArray
                    ? new[] {LeftExpression.Type.GetElementType()!}
                    : new[] {LeftExpression.Type.GenericTypeArguments[0] },
                LeftExpression);
            var isEmpty = Expression.Not(Expression.IsTrue(anyCheck));
            return Expression.OrElse(isNull, isEmpty);
        }
    }
}