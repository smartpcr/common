// -----------------------------------------------------------------------
// <copyright file="KustoTable.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Kusto;

using System.Collections.Generic;
using System.Text;

public class KustoTable
{
    public string Name { get; set; }
    public List<KustoColumn> Columns { get; set; }
    public string Folder { get; set; }
    public string DocString { get; set; }
    public KustoTableRetentionPolicy RetentionPolicy { get; set; }

    public override string ToString()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append($".create-merge table {Name} (\n");
        for (var i = 0; i < Columns.Count; i++)
        {
            var col = Columns[i];
            stringBuilder.Append($"{col.Name}:{col.CslType}");
            if (i < Columns.Count - 1)
            {
                stringBuilder.Append(",\n");
            }
            else
            {
                stringBuilder.Append(")");
            }
        }

        return stringBuilder.ToString();
    }
}