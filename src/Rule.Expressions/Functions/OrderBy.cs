// -----------------------------------------------------------------------
// <copyright file="OrderBy.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions.Functions
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using ArgumentException = System.ArgumentException;
    using NotSupportedException = System.NotSupportedException;

    public class OrderBy : FunctionExpression
    {
        private readonly string orderByField;
        private readonly string methodName;

        public OrderBy(Expression target, FunctionName funcName, params string[] args) : base(target, funcName, args)
        {
            if (args != null && args.Length > 1)
            {
                throw new ArgumentException($"exactly zero or one argument expected for function '{funcName}'");
            }

            if (args?.Length == 1)
            {
                orderByField = args[0];
            }

            if (funcName == FunctionName.OrderBy)
            {
                methodName = "OrderBy";
            }
            else if (funcName == FunctionName.OrderByDesc)
            {
                methodName = "OrderByDescending";
            }
            else
            {
                throw new NotSupportedException($"invalid {funcName} for order by");
            }
        }

        public override Expression Build()
        {
            Type? itemType = null;
            if (Target.Type.IsGenericType)
            {
                itemType = Target.Type.GetGenericArguments()[0];
            }
            else if (Target.Type.IsArray)
            {
                itemType = Target.Type.GetElementType()!;
            }

            if (itemType == null)
            {
                throw new InvalidOperationException($"target type '{Target.Type.Name}' of select function is not supported");
            }

            var argParameter = Expression.Parameter(itemType, "_");

            if (string.IsNullOrEmpty(orderByField))
            {
                var selector = Expression.Lambda(argParameter, argParameter);

                return Expression.Call(
                    typeof(Enumerable),
                    methodName,
                    new[] {itemType, itemType},
                    Target,
                    selector);
            }

            var prop = itemType.GetMappedProperty(orderByField);
            var propExpression = Expression.Property(argParameter, prop);
            Expression selectorExpression = Expression.Lambda(propExpression, argParameter);

            return Expression.Call(
                typeof(Enumerable),
                methodName,
                new[] {itemType, propExpression.Type},
                Target,
                selectorExpression);
        }
    }
}