// -----------------------------------------------------------------------
// <copyright file="Ago.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions.Functions
{
    using System;
    using System.Linq.Expressions;
    using System.Text.RegularExpressions;

    public class Ago : FunctionExpression
    {
        private readonly TimeSpan span;
        private static readonly Regex argRegex = new Regex(@"(\d+)(m|h|d)", RegexOptions.Compiled);

        public Ago(Expression target, params string[] args) : base(target, FunctionName.Ago, args)
        {
            if (args == null || args.Length != 1)
            {
                throw new ArgumentException($"Exactly one argument is required for function '{FunctionName.Ago}'");
            }
            var funcArg = args[0];
            var match = argRegex.Match(funcArg);
            if (!match.Success)
            {
                throw new InvalidOperationException($"invalid arg '{funcArg}' for function {FunctionName.Ago}");
            }

            var number = int.Parse(match.Groups[1].Value);
            switch (match.Groups[2].Value)
            {
                case "m":
                    span = TimeSpan.FromMinutes(0 - number);
                    break;
                case "h":
                    span = TimeSpan.FromHours(0 - number);
                    break;
                case "d":
                    span = TimeSpan.FromDays(0 - number);
                    break;
                default:
                    throw new InvalidOperationException($"invalid arg '{funcArg}' for function {FunctionName.Ago}");
            }
        }

        public override Expression? Build()
        {
            var now = Expression.Constant(DateTime.UtcNow);
            var spanExpr = Expression.Constant(span);
            var method = typeof(DateTime).GetMethod("Add");
            if (method == null)
            {
                throw new InvalidOperationException("method 'Add' not found on DateTime type");
            }

            return Expression.Call(now, method, spanExpr);
        }
    }
}