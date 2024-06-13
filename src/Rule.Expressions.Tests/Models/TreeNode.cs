// -----------------------------------------------------------------------
// <copyright file="TreeNode.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Rule.Expressions.Tests.Models
{
    using Newtonsoft.Json;

    public class TreeNode
    {
        public string Id { get; set; }
        public TreeNode Parent { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}