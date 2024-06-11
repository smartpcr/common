// -----------------------------------------------------------------------
// <copyright file="Select.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions.Functions
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    public class Select : FunctionExpression
    {
        private readonly MethodInspect callInfo;
        private readonly Expression parent;
        private readonly string selectionPath;

        public Select(Expression target, params string[] args)
            : base(target, FunctionName.Select, args)
        {
            if (args == null || args.Length != 1)
            {
                throw new ArgumentException($"Exactly one argument is required for function '{FunctionName.Select}'");
            }

            parent = target;
            selectionPath = args[0];
            callInfo = new MethodInspect("Select", parent.Type, typeof(string), typeof(Enumerable));
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
                itemType = callInfo.TargetType.GetElementType()!;
            }

            if (itemType == null)
            {
                throw new InvalidOperationException($"target type '{callInfo.TargetType.Name}' of select function is not supported");
            }

            var paramExpression = Expression.Parameter(itemType, "item");
            var propExpression = paramExpression.EvaluateExpression(selectionPath);

            Expression selectorExpression = Expression.Lambda(propExpression!, paramExpression);

            MethodInfo? selectMethod = null;
            foreach (var m in typeof(Enumerable).GetMethods().Where(m => m.Name == "Select"))
            {
                foreach (var p in m.GetParameters().Where(p => p.Name != null && p.Name.Equals("selector")))
                {
                    if (p.ParameterType.GetGenericArguments().Length == 2)
                        selectMethod = (MethodInfo) p.Member;
                }
            }

            if (selectMethod == null) throw new InvalidOperationException("Failed to get generic select method");

            var genericSelectMethod = selectMethod.MakeGenericMethod(itemType, propExpression!.Type);
            var selectExpression = Expression.Call(
                null,
                genericSelectMethod,
                parent,
                selectorExpression);

            return selectExpression;
        }
    }
}