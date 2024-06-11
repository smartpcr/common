// -----------------------------------------------------------------------
// <copyright file="FunctionExpression.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions.Functions
{
    using System.Linq.Expressions;

    public abstract class FunctionExpression
    {
        protected Expression Target { get; }
        protected FunctionName FuncName { get; }
        public string?[] Args { get; }

        protected FunctionExpression(Expression target, FunctionName funcName, params string?[] args)
        {
            Target = target;
            FuncName = funcName;
            Args = args;
        }

        public abstract Expression? Build();
    }
}