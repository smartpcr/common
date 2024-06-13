// -----------------------------------------------------------------------
// <copyright file="IOperatorExpression.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions.Operators
{
    using System.Linq.Expressions;

    public interface IOperatorExpression
    {
        Expression Create();
    }
}