// -----------------------------------------------------------------------
// <copyright file="AnyOfExpression.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using Newtonsoft.Json;

    public class AnyOfExpression : IConditionExpression
    {
        [JsonProperty(Required = Required.Always)]
        public IConditionExpression[] AnyOf { get; set; }

        public Expression Process(ParameterExpression parameterExpression, Type parameterType)
        {
            if (AnyOf.Length == 0)
            {
                var rightExpression = Expression.Constant(null, parameterExpression.Type);
                var notNullExpression = Expression.Not(Expression.Equal(parameterExpression, rightExpression));
                return notNullExpression;
            }

            if (AnyOf.Length == 1) return AnyOf[0].Process(parameterExpression, parameterType);
            var expression = Expression.OrElse(AnyOf[0].Process(parameterExpression, parameterType),
                AnyOf[1].Process(parameterExpression, parameterType));
            for (var i = 2; i < AnyOf.Length; i++)
                expression = Expression.OrElse(expression, AnyOf[i].Process(parameterExpression, parameterType));

            return expression;
        }

        public bool IsEmpty => !(AnyOf.Length > 0 && AnyOf.All(expr => !expr.IsEmpty));
    }
}