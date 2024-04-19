// -----------------------------------------------------------------------
// <copyright file="NonEmptyValidationAttribute.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Config.Validation;

using System.Collections;
using System.ComponentModel.DataAnnotations;

public class NonEmptyValidationAttribute : ValidationAttribute
{
    public override bool IsValid(object value)
    {
        if (value is IList list)
        {
            return list.Count > 0;
        }

        if (value is IDictionary dict)
        {
            return dict.Count > 0;
        }

        return false;
    }
}