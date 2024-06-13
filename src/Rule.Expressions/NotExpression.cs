// -----------------------------------------------------------------------
// <copyright file="NotExpression.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions
{
    using System;
    using System.Linq.Expressions;
    using Newtonsoft.Json;

    public class NotExpression : IConditionExpression
    {
        [JsonProperty(Required = Required.Always)]
        public IConditionExpression Not { get; set; }

        public Expression Process(ParameterExpression parameterExpression, Type parameterType)
        {
            return Expression.Not(Not.Process(parameterExpression, parameterType));
        }

        public bool IsEmpty => Not.IsEmpty;
    }
}