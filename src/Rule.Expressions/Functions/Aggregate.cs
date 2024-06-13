// -----------------------------------------------------------------------
// <copyright file="Aggregate.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions.Functions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    public class Aggregate : FunctionExpression
    {
        private readonly MethodInspect callInfo;
        private readonly string? selectionPath;

        // decimal, int, double are all supported, arg type is retrieved from caller inside constructor
        private readonly MethodInspect[] supportedMethods =
        {
            new MethodInspect("Count", typeof(IEnumerable<string>), typeof(string), typeof(Enumerable)),
            new MethodInspect("Count", typeof(string[]), typeof(string), typeof(Enumerable)),
            new MethodInspect("Count", typeof(IEnumerable<decimal>), typeof(decimal), typeof(Enumerable)),
            new MethodInspect("Count", typeof(decimal[]), typeof(decimal), typeof(Enumerable)),
            new MethodInspect("Count", typeof(Enumerable), typeof(object), typeof(Enumerable)),
            new MethodInspect("Count", typeof(object[]), typeof(object), typeof(Enumerable)),
            new MethodInspect("DistinctCount", typeof(IEnumerable<string>), typeof(string), typeof(Enumerable)),
            new MethodInspect("DistinctCount", typeof(string[]), typeof(string), typeof(Enumerable)),
            new MethodInspect("DistinctCount", typeof(IEnumerable<decimal>), typeof(decimal), typeof(Enumerable)),
            new MethodInspect("DistinctCount", typeof(decimal[]), typeof(decimal), typeof(Enumerable)),
            new MethodInspect("DistinctCount", typeof(Enumerable), typeof(object), typeof(Enumerable)),
            new MethodInspect("DistinctCount", typeof(object[]), typeof(object), typeof(Enumerable)),

            new MethodInspect("Average", typeof(IEnumerable<decimal>), typeof(decimal), typeof(Enumerable)),
            new MethodInspect("Average", typeof(decimal[]), typeof(decimal), typeof(Enumerable)),
            new MethodInspect("Max", typeof(IEnumerable<decimal>), typeof(decimal), typeof(Enumerable)),
            new MethodInspect("Max", typeof(decimal[]), typeof(decimal), typeof(Enumerable)),
            new MethodInspect("Min", typeof(IEnumerable<decimal>), typeof(decimal), typeof(Enumerable)),
            new MethodInspect("Min", typeof(decimal[]), typeof(decimal), typeof(Enumerable)),
            new MethodInspect("Sum", typeof(IEnumerable<decimal>), typeof(decimal), typeof(Enumerable)),
            new MethodInspect("Sum", typeof(decimal[]), typeof(decimal), typeof(Enumerable))
        };

        public Aggregate(Expression target, FunctionName funcName, params string?[] args)
            : base(target, funcName, args)
        {
            selectionPath = args.Length > 0 ? args[0] : null;
            var targetType = target.Type;
            var methodName = funcName.ToString();
            var callInfo1 = supportedMethods.FirstOrDefault(m =>
                m.TargetType == targetType && m.MethodName.Equals(methodName, StringComparison.OrdinalIgnoreCase));
            if (callInfo1 == null)
            {
                if (targetType.IsGenericType)
                {
                    var argType = targetType.GetGenericArguments()[0];
                    callInfo1 = new MethodInspect(methodName, targetType, argType, typeof(Enumerable));
                }
                else if (targetType.IsArray)
                {
                    var argType = targetType.GetElementType();
                    callInfo1 = new MethodInspect(methodName, targetType, argType!, typeof(Enumerable));
                }
            }

            callInfo = callInfo1 ?? throw new NotSupportedException("Operator in condition is not supported for field type");
        }

        public override Expression? Build()
        {
            switch (callInfo.MethodName)
            {
                case "DistinctCount":
                    return CreateDistinctCount();
                case "Count":
                    return Expression.Call(
                        callInfo.ExtensionType,
                        callInfo.MethodName,
                        new[] {callInfo.ArgumentType},
                        Target);
                default:
                    return CreateAggregateFunction();
            }
        }

        private MethodCallExpression? CreateDistinctCount()
        {
            Type[] typeArgument;
            if (callInfo.ArgumentType == typeof(string[]) || callInfo.ArgumentType == typeof(decimal[]))
            {
                typeArgument = new[] {callInfo.ArgumentType.GetElementType()!};
            }
            else
            {
                typeArgument = new[] {callInfo.ArgumentType};
            }

            var distinct = Expression.Call(
                callInfo.ExtensionType,
                "Distinct",
                typeArgument,
                Target);
            var count = Expression.Call(
                callInfo.ExtensionType,
                "Count",
                typeArgument,
                distinct);
            return count;
        }

        /// <summary>
        /// avg, sum, min, max all requires the source to be non empty.
        /// </summary>
        /// <returns></returns>
        private Expression? CreateAggregateFunction()
        {
            var test = CheckNotNullAndNotEmpty(Target);
            MethodCallExpression whenTrue;

            if (string.IsNullOrEmpty(selectionPath))
            {
                whenTrue = Expression.Call(
                    callInfo.ExtensionType,
                    callInfo.MethodName,
                    null,
                    Target);
            }
            else
            {
                var propInfo = callInfo.ArgumentType.GetProperty(selectionPath);
                if (propInfo == null)
                {
                    throw new InvalidOperationException($"unable to access property '{selectionPath}' on type '{callInfo.ArgumentType.Name}'");
                }

                var itemParameter = Expression.Parameter(callInfo.ArgumentType, "p");
                var propAccess = Expression.MakeMemberAccess(itemParameter, propInfo);
                var selectorExpr = Expression.Lambda(propAccess, itemParameter);
                whenTrue = Expression.Call(
                    callInfo.ExtensionType,
                    callInfo.MethodName,
                    new[] {callInfo.ArgumentType},
                    Target,
                    selectorExpr);
            }

            LabelTarget returnTarget = Expression.Label(whenTrue.Type);
            Expression iftrue = Expression.Return(returnTarget, whenTrue);
            Expression iffalse = Expression.Return(returnTarget, Expression.Default(whenTrue.Type));
            return Expression.Block(Expression.IfThenElse(test, iftrue, iffalse), Expression.Label(returnTarget, Expression.Default(whenTrue.Type)));
        }

        private Expression CheckNotNullAndNotEmpty(Expression targetExpression)
        {
            var isNotNull = Expression.NotEqual(targetExpression, Expression.Constant(null, targetExpression.Type));
            var isNotEmpty = Expression.Call(
                typeof(Enumerable),
                "Any",
                targetExpression.Type.IsArray
                    ? new[] {targetExpression.Type.GetElementType()!}
                    : new[] {targetExpression.Type.GenericTypeArguments[0] },
                targetExpression);
            return Expression.AndAlso(isNotNull, isNotEmpty);
        }
    }
}