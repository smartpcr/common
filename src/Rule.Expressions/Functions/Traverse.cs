// -----------------------------------------------------------------------
// <copyright file="Traverse.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions.Functions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    public class Traverse : FunctionExpression
    {
        private readonly PropertyInfo traverseProp;
        private readonly PropertyInfo uniqueProp;
        private readonly int maxStep = -1;

        public Traverse(Expression target, FunctionName funcName, params string[] args) : base(target, funcName, args)
        {
            if (args == null || args.Length < 2 || args.Length > 3)
            {
                throw new ArgumentException($"function '{funcName}' requires 2 or 3 arguments");
            }

            var propName = args[0];
            traverseProp = Target.Type.GetMappedProperty(propName);
            if (traverseProp == null)
            {
                throw new InvalidOperationException($"Unable to find property '{propName}' from type '{Target.Type.Name}'");
            }

            if (traverseProp.PropertyType != target.Type)
            {
                throw new InvalidOperationException($"expect property '{propName}' type to be '{target.Type.Name}', but found '{traverseProp.PropertyType.Name}'");
            }

            var uniqueFieldName = args[1];
            uniqueProp = Target.Type.GetMappedProperty(uniqueFieldName);
            if (uniqueProp == null)
            {
                throw new InvalidOperationException($"Unable to find property '{uniqueFieldName}' from type '{Target.Type.Name}'");
            }
            if (uniqueProp.PropertyType != typeof(string))
            {
                throw new InvalidOperationException($"expect property '{uniqueFieldName}' type to be '{nameof(String)}', but found '{uniqueProp.PropertyType.Name}'");
            }

            if (args.Length == 3)
            {
                maxStep = int.Parse(args[2]);
            }
        }

        [SuppressMessage("Microsoft.Design", "SA1115:ListsAreStrongTyped", Justification = "as design")]
        public override Expression? Build()
        {
            ParameterExpression parentParam = Expression.Parameter(Target.Type, "parent");
            ParameterExpression pathVar = Expression.Variable(typeof(List<string>), "path");
            ParameterExpression currentVar = Expression.Variable(Target.Type, "current");
            ParameterExpression currentStepVar = Expression.Variable(typeof(int), "currentStep");
            ParameterExpression haveLoopVar = Expression.Variable(typeof(bool), "haveLoop");

            var argumentTypes = new[] { typeof(IEnumerable<string>).GenericTypeArguments[0] };
            MethodInfo? addMethod = typeof(List<string>).GetMethod("Add");

            LabelTarget label = Expression.Label(typeof(List<string>));

            var getTraversalPath = Expression.Block(
                new[] { parentParam, pathVar, currentVar, currentStepVar, haveLoopVar },
                Expression.Assign(pathVar, Expression.Constant(new List<string>())),
                Expression.Assign(currentVar, Target),
                Expression.Call(
                    pathVar,
                    addMethod!,
                    Expression.Property(currentVar, uniqueProp)),
                Expression.Assign(currentStepVar, Expression.Constant(0)),
                Expression.Assign(haveLoopVar, Expression.Constant(false)),
                Expression.Assign(parentParam, Expression.Property(currentVar, traverseProp)),
                Expression.Loop(
                    Expression.IfThenElse(
                        Expression.AndAlso(
                            Expression.AndAlso(
                                Expression.IsFalse(haveLoopVar),
                                Expression.NotEqual(parentParam, Expression.Constant(null, typeof(object)))
                            ),
                            Expression.OrElse(
                                Expression.LessThanOrEqual(Expression.Constant(maxStep), Expression.Constant(0)),
                                Expression.LessThan(currentStepVar, Expression.Constant(maxStep))
                            )
                        ),
                        Expression.Block(
                            Expression.PostIncrementAssign(currentStepVar),
                            Expression.Assign(currentVar, parentParam),
                            Expression.Assign(parentParam, Expression.Property(parentParam, traverseProp)),

                            Expression.IfThenElse(
                                Expression.Call(
                                    typeof(Enumerable),
                                    "Contains",
                                    argumentTypes,
                                    pathVar,
                                    Expression.Property(currentVar, uniqueProp)),
                                Expression.Block(
                                    Expression.Assign(haveLoopVar, Expression.Constant(true)),
                                    Expression.Call(
                                        pathVar,
                                        addMethod!,
                                        Expression.Constant("!!")) // indicates a loop
                                ),
                                Expression.Block(
                                    Expression.Call(
                                        pathVar,
                                        addMethod!,
                                        Expression.Property(currentVar, uniqueProp))
                                )
                            )
                        ),
                        Expression.Break(label, pathVar)
                    ), label
                ),
                pathVar // return
            );

            return getTraversalPath;
        }
    }
}