// -----------------------------------------------------------------------
// <copyright file="ExpressionEvaluatorExtension.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions.Evaluators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text.RegularExpressions;
    using Functions;
    using Macros;

    public static class ExpressionEvaluatorExtension
    {
        public static Expression EvaluateExpression(this ParameterExpression contextExpression, string propPath, bool handleNullableType = true)
        {
            var parts = SplitPropPath(propPath);
            Expression targetExpression = contextExpression;
            foreach (var part in parts)
            {
                if (part.Equals("Self", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (targetExpression.TryFindMacro(part, out var macroExpr))
                    targetExpression = macroExpr!;
                else if (targetExpression.TryFindFunction(part, out var funcExpr))
                    targetExpression = funcExpr!;
                else if (targetExpression.TryFindIndexerField(part, out var arrayItemExpr))
                    targetExpression = arrayItemExpr!;
                else if (targetExpression.TryFindProperty(part, out var propExpression))
                    targetExpression = propExpression!;
                else
                    throw new InvalidOperationException($"failed to evaluate part '{part}' on type {targetExpression.Type.Name}");

                if (HandleEnumExpression(targetExpression, out var enumExpression)) targetExpression = enumExpression!;

                if (handleNullableType && HandleNullableType(targetExpression, out var valueExpression)) targetExpression = valueExpression!;
            }

            return targetExpression;
        }

        public static Expression AddEnumToStringConvert(this Expression targetExpression)
        {
            var addToStringMethod = false;
            var isNullable = false;
            var targetType = targetExpression.Type;
            if (targetType.IsEnum)
            {
                addToStringMethod = true;
            }
            else
            {
                var underlyingType = Nullable.GetUnderlyingType(targetType);
                if (underlyingType?.IsEnum == true)
                {
                    addToStringMethod = true;
                    isNullable = true;
                }
            }

            if (addToStringMethod)
            {
                var toStringMethod = targetType.GetMethod("ToString", Type.EmptyTypes);
                if (toStringMethod == null) throw new Exception($"type {targetType.Name} doesn't have ToString method");

                if (isNullable)
                {
                    Type stringType = typeof(string);
                    LabelTarget returnTarget = Expression.Label(stringType);
                    var testIsNull = Expression.Equal(targetExpression, Expression.Constant(null, stringType));
                    Expression iftrue = Expression.Return(returnTarget, Expression.Constant(null, stringType));
                    Expression iffalse = Expression.Return(returnTarget, Expression.Call(targetExpression, toStringMethod));
                    return Expression.Block(
                        Expression.IfThenElse(testIsNull, iftrue, iffalse),
                        Expression.Label(returnTarget, Expression.Constant(null, stringType)));
                }

                var toStringCall = Expression.Call(targetExpression, toStringMethod);
                return toStringCall;
            }

            return targetExpression;
        }

        public static Expression AddValueWithNullableNumberType(this Expression targetExpression)
        {
            var targetType = targetExpression.Type;
            var underlyingType = Nullable.GetUnderlyingType(targetType);
            if (underlyingType != null)
            {
                var toValueCall = Expression.Property(targetExpression, "Value");
                return toValueCall;
            }

            return targetExpression;
        }

        public static bool AddNotNullCheck(this Expression targetExpression, out Expression? notNullCheckExpression)
        {
            notNullCheckExpression = null;
            var targetType = targetExpression.Type;
            if (!targetType.IsPrimitiveType() && Nullable.GetUnderlyingType(targetType) == null)
            {
                var nullExpr = Expression.Constant(null, typeof(object));
                notNullCheckExpression = Expression.Not(Expression.Equal(targetExpression, nullExpr));
                return true;
            }

            return false;
        }

        /// <summary>
        /// using "." as delimiter but skip anything inside parenthesis
        /// note: there can be nested function inside another function
        /// </summary>
        /// <param name="propPath"></param>
        /// <returns></returns>
        public static string[] SplitPropPath(this string propPath)
        {
            var parts = new List<string>();
            var left = propPath.IndexOf("(", 0, StringComparison.OrdinalIgnoreCase);
            if (left < 0)
            {
                return propPath.Split(new[] {'.'}, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim()).ToArray();
            }

            var stack = new Stack<int>();
            var queue = new Queue<(int left, int right)>();
            for (var i = 0; i < propPath.Length; i++)
            {
                var c = propPath[i];
                if (c == '(')
                {
                    stack.Push(i);
                }
                else if (c == ')')
                {
                    left = stack.Pop();
                    if (stack.Count == 0)
                    {
                        queue.Enqueue((left, i));
                    }
                }
            }

            if (stack.Count > 0)
            {
                throw new InvalidOperationException($"imbalanced expression found: '(' at {stack.Pop()} is not matched");
            }

            if (queue.Count <= 0)
            {
                throw new InvalidOperationException($"unable to capture any enclosed parenthesis");
            }

            var prevPos = 0;
            while (queue.Count > 0)
            {
                var grouped = queue.Dequeue();
                if (grouped.left > prevPos)
                {
                    var pathBeforeGroup = propPath.Substring(prevPos, grouped.left - prevPos);
                    var partsBeforeGroup = pathBeforeGroup.Split(new[] {'.'}, StringSplitOptions.RemoveEmptyEntries);
                    for (var i = 0; i < partsBeforeGroup.Length - 1; i++)
                    {
                        parts.Add(partsBeforeGroup[i]);
                    }

                    var funcName = partsBeforeGroup[^1];
                    var funcArgs = propPath.Substring(grouped.left, grouped.right + 1 - grouped.left);
                    var funcDef = $"{funcName}{funcArgs}";
                    parts.Add(funcDef);
                    prevPos = grouped.right + 1;
                }
                else
                {
                    throw new InvalidOperationException($"missing function name at {grouped.left}");
                }
            }

            if (prevPos < propPath.Length)
            {
                var pathAfterGroup = propPath.Substring(prevPos);
                parts.AddRange(pathAfterGroup.Split(new[] {'.'}, StringSplitOptions.RemoveEmptyEntries));
            }

            return parts.ToArray();
        }

        private static bool TryFindMacro(
            this Expression parentExpression,
            string field,
            out Expression? macroExpression)
        {
            macroExpression = null;

            var methodRegex = new Regex(@"^(\w+)\(([^\(\)]*)\)$");
            if (methodRegex.IsMatch(field))
            {
                var macroName = methodRegex.Match(field).Groups[1].Value;
                var macroArgs = methodRegex.Match(field).Groups[2].Value;
                var args = macroArgs.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()).ToArray();
                var extensionMethods = parentExpression.Type.GetExtensionMethods().ToArray();
                if (extensionMethods.Any())
                {
                    var extensionMethod = extensionMethods.FirstOrDefault(m => m.Name == macroName);
                    if (extensionMethod != null)
                    {
                        var methodParameters = extensionMethod.GetParameters();
                        if (methodParameters.Length >= 1 && methodParameters[0].ParameterType == parentExpression.Type)
                        {
                            var macroCreator = new MacroExpressionCreator(parentExpression, extensionMethod, args);
                            macroExpression = macroCreator.CreateMacroExpression();
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool TryFindFunction(
            this Expression parentExpression,
            string field,
            out Expression? functionExpression)
        {
            functionExpression = null;
            var functionRegexPatterns = FunctionNameExtension.GetFunctionNameRegexPatterns();
            foreach (var pattern in functionRegexPatterns)
            {
                var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                var match = regex.Match(field);
                if (match.Success)
                {
                    var functionName = match.Groups[1].Value;
                    var functionArg = match.Groups[2].Value;
                    var args = functionArg.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()).ToArray();
                    var funcName = (FunctionName)Enum.Parse(typeof(FunctionName), functionName, true);
                    var funcExpr = new FunctionExpressionCreator().Create(parentExpression, funcName, args);
                    functionExpression = funcExpr.Build();
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindIndexerField(
            this Expression parentExpression,
            string field,
            out Expression? arrayExpression)
        {
            // Indexer access field should be of the form property['<key>'] or property[<key>]
            arrayExpression = null;
            var leftBracketIndex = field.IndexOf("[", StringComparison.OrdinalIgnoreCase);
            if (leftBracketIndex > 0)
            {
                var rightBracketIndex = field.IndexOf("]", leftBracketIndex, StringComparison.OrdinalIgnoreCase);
                if (rightBracketIndex != -1)
                {
                    var key = field.Substring(leftBracketIndex + 1, rightBracketIndex - leftBracketIndex - 1);
                    key = key.Trim('\'');
                    var propertyName = field.Substring(0, leftBracketIndex);
                    if (!parentExpression.TryFindProperty(propertyName, out var propExpression) ||
                        propExpression == null)
                    {
                        throw new InvalidOperationException(
                            $"failed to get array property {propertyName} on type {parentExpression.Type.Name}");
                    }

                    if (int.TryParse(key, out var index))
                    {
                        var collectionType = propExpression.Type;
                        if (collectionType.IsGenericType)
                        {
                            propExpression = Expression.Call(
                                typeof(Enumerable),
                                "ToArray",
                                new[] { collectionType.GetGenericArguments()[0] },
                                propExpression);
                            arrayExpression = Expression.ArrayIndex(propExpression, Expression.Constant(index));
                        }
                        else
                        {
                            arrayExpression = Expression.ArrayIndex(propExpression, Expression.Constant(index));
                        }
                    }
                    else
                    {
                        var keyExpression = Expression.Constant(key);
                        arrayExpression = Expression.Property(propExpression, "Item", keyExpression);
                    }

                    return true;
                }
            }

            return false;
        }

        private static bool TryFindProperty(
            this Expression parentExpression,
            string field,
            out Expression? propExpression)
        {
            propExpression = null;
            var prop = parentExpression.Type.GetMappedProperty(field);
            propExpression = Expression.Property(parentExpression, prop);
            return true;
        }

        private static bool HandleEnumExpression(
            this Expression parentExpression,
            out Expression? enumExpression)
        {
            enumExpression = null;
            var fieldType = parentExpression.Type;
            var underlyingType = Nullable.GetUnderlyingType(fieldType);
            if (fieldType.IsEnum || underlyingType?.IsEnum == true)
            {
                var toStringMethod = fieldType.GetMethod("ToString", Type.EmptyTypes);
                if (toStringMethod == null) throw new Exception($"type {fieldType.Name} doesn't have ToString method");

                if (underlyingType != null)
                {
                    Type stringType = typeof(string);
                    LabelTarget returnTarget = Expression.Label(stringType);
                    var testIsNull = Expression.Equal(parentExpression, Expression.Constant(null, stringType));
                    Expression iftrue = Expression.Return(returnTarget, Expression.Constant(null, stringType));
                    Expression iffalse = Expression.Return(returnTarget, Expression.Call(parentExpression, toStringMethod));
                    enumExpression = Expression.Block(
                        Expression.IfThenElse(testIsNull, iftrue, iffalse),
                        Expression.Label(returnTarget, Expression.Constant(null, stringType)));
                    return true;
                }

                enumExpression = Expression.Call(parentExpression, toStringMethod);
                return true;
            }

            return false;
        }

        private static bool HandleNullableType(
            this Expression parentExpression,
            out Expression? valueExpression)
        {
            valueExpression = null;
            var fieldType = parentExpression.Type;
            var underlyingType = Nullable.GetUnderlyingType(fieldType);
            if (underlyingType?.IsNumericType() == true)
            {
                valueExpression = Expression.Property(parentExpression, "Value");
                return true;
            }

            return false;
        }
    }
}