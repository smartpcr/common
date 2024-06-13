// -----------------------------------------------------------------------
// <copyright file="IExpressionEvaluator.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions.Evaluators
{
    using System;

    public interface IExpressionEvaluator
    {
        Func<T, bool> Evaluate<T>(IConditionExpression conditionExpression) where T : class;

        Delegate Evaluate(IConditionExpression conditionExpression, Type contextType);
    }
}