// -----------------------------------------------------------------------
// <copyright file="KustoColumnAttribute.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Kusto;

using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class KustoColumnAttribute : Attribute
{
    public string Name { get; set; }
    public Type Type { get; set; }
    public string CslType { get; set; }

    public KustoColumnAttribute(string name, Type type, string cslType)
    {
        Name = name;
        Type = type;
        CslType = cslType;
    }
}