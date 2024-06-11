// -----------------------------------------------------------------------
// <copyright file="MethodInspect.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions
{
    using System;

    public class MethodInspect
    {
        public MethodInspect(string methodName, Type targetType, Type argumentType, Type extensionType)
        {
            MethodName = methodName;
            TargetType = targetType;
            ArgumentType = argumentType;
            ExtensionType = extensionType;
        }

        public string MethodName { get; }
        public Type TargetType { get; }
        public Type ArgumentType { get; }
        public Type ExtensionType { get; }
    }
}