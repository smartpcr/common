// -----------------------------------------------------------------------
// <copyright file="IConditionExpression.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions
{
    using System;
    using System.Linq.Expressions;

    public interface IConditionExpression
    {
        Expression Process(ParameterExpression parameterExpression, Type parameterType);

        bool IsEmpty { get; }
    }
}