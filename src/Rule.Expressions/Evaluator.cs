// -----------------------------------------------------------------------
// <copyright file="Evaluator.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions;

using System;
using System.Linq;
using System.Linq.Expressions;

public static class Evaluator
{
    public static Expression Process<T>(ParameterExpression contextParameter, string propPath)
    {
        var propParts = SplitPropPath(propPath);
        Expression output = contextParameter;
        foreach (var part in propParts)
        {
            if (output.TryFindIndexerField(part, out var arrayItemExpr))
            {
                output = arrayItemExpr;
            }
            else if (output.TryFindProperty(part, out var propExpr))
            {
                output = propExpr;
            }
            else
            {
                throw new InvalidOperationException($"failed to evaluate part {part} on target type {output.Type.Name}");
            }
        }

        return output;
    }

    public static ParameterExpression ContextParameter<T>() where T : class
    {
        return Expression.Parameter(typeof(T), "ctx");
    }

    public static Func<T1, T2> Evaluate<T1, T2>(string propPath) where T1 : class
    {
        var contextParameter = ContextParameter<T1>();
        var expression = Process<T1>(contextParameter, propPath);
        var @delegate = Expression.Lambda<Func<T1, T2>>(expression, contextParameter);
        var func = @delegate.Compile();
        return func;
    }

    private static string[] SplitPropPath(string propPath)
    {
        return propPath.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim())
            .Where(p => !string.IsNullOrEmpty(p)).ToArray();
    }

    private static bool TryFindProperty(this Expression parentExpression, string propName, out Expression propExpression)
    {
        propExpression = null;
        var prop = parentExpression.Type.GetProperty(propName);
        if (prop != null)
        {
            propExpression = Expression.Property(parentExpression, prop);
            return true;
        }

        return false;
    }

    private static bool TryFindIndexerField(
        this Expression parentExpression,
        string field,
        out Expression arrayExpression)
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
}
