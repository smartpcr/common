// -----------------------------------------------------------------------
// <copyright file="OperatorExpression.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions.Operators
{
    using System.Linq.Expressions;

    public abstract class OperatorExpression : IOperatorExpression
    {
        protected Expression? LeftExpression { get; set; }
        protected Expression? RightExpression { get; set; }

        protected OperatorExpression(Expression? leftExpression, Expression? rightExpression)
        {
            LeftExpression = leftExpression;
            RightExpression = rightExpression;
        }

        public abstract Expression Create();
    }
}