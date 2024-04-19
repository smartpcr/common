// -----------------------------------------------------------------------
// <copyright file="KustoFunction.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Kusto;

public class KustoFunction
{
    public string Name { get; set; }
    public string? Parameters { get; set; }
    public string? Body { get; set; }
    public string? Folder { get; set; }
    public string? DocString { get; set; }

    public override string ToString()
    {
        return $".create-or-alter function with (folder = \"{Folder}\", docstring = \"{DocString}\") {Name}{Parameters}" +
               "\n" + Body + "\n";
    }
}