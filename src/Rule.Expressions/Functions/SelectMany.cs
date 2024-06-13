// -----------------------------------------------------------------------
// <copyright file="SelectMany.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions.Functions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    public class SelectMany : FunctionExpression
    {
        private readonly MethodInspect callInfo;
        private readonly string selectionPath;

        public SelectMany(Expression target, params string[] args) : base(target, FunctionName.SelectMany, args)
        {
            if (args == null || args.Length != 1)
            {
                throw new ArgumentException($"Exactly one argument is required for function '{FunctionName.SelectMany}'");
            }

            selectionPath = args[0];
            callInfo = new MethodInspect("SelectMany", target.Type, typeof(string), typeof(Enumerable));
        }

        public override Expression? Build()
        {
            Type? itemType = null;
            if (callInfo.TargetType.IsGenericType)
            {
                itemType = callInfo.TargetType.GetGenericArguments()[0];
            }
            else if (callInfo.TargetType.IsArray)
            {
                itemType = callInfo.TargetType.GetElementType();
            }

            if (itemType == null)
            {
                throw new InvalidOperationException($"target type '{callInfo.TargetType.Name}' of SelectMany function is not supported");
            }

            var paramExpression = Expression.Parameter(itemType, "item");
            var propExpression = paramExpression.EvaluateExpression(selectionPath);
            var propItemType = propExpression!.Type.GetGenericArguments()[0];
            Type enumerableType = typeof(IEnumerable<>).MakeGenericType(propItemType);
            Type delegateType = typeof(Func<,>).MakeGenericType(itemType, enumerableType);
            Expression selectorExpression = Expression.Lambda(delegateType, propExpression, paramExpression);

            var selectExpression = Expression.Call(
                typeof(Enumerable),
                "SelectMany",
                new[] {itemType, propItemType},
                Target,
                selectorExpression);

            return selectExpression;
        }
    }
}