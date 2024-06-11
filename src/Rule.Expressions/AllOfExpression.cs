// -----------------------------------------------------------------------
// <copyright file="AllOfExpression.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using Newtonsoft.Json;

    public class AllOfExpression : IConditionExpression
    {
        [JsonProperty(Required = Required.Always)]
        public IConditionExpression[] AllOf { get; set; }

        public Expression Process(ParameterExpression parameterExpression, Type parameterType)
        {
            if (AllOf.Length == 0)
            {
                var rightExpression = Expression.Constant(null, parameterExpression.Type);
                var notNullExpression = Expression.Not(Expression.Equal(parameterExpression, rightExpression));
                return notNullExpression;
            }

            if (AllOf.Length == 1) return AllOf[0].Process(parameterExpression, parameterType);

            var expression = Expression.AndAlso(AllOf[0].Process(parameterExpression, parameterType),
                AllOf[1].Process(parameterExpression, parameterType));
            for (var i = 2; i < AllOf.Length; i++)
            {
                expression = Expression.AndAlso(expression, AllOf[i].Process(parameterExpression, parameterType));
            }
            return expression;
        }

        public bool IsEmpty => !(AllOf.Length > 0 && AllOf.All(expr => !expr.IsEmpty));
    }
}