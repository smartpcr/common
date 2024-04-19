// -----------------------------------------------------------------------
// <copyright file="DatabaseAccountListKeysResult.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.DocDb;

public class DatabaseAccountListKeysResult
{
    public string PrimaryMasterKey { get; set; }
    public string PrimaryReadonlyMasterKey { get; set; }
    public string SecondaryMasterKey { get; set; }
    public string SecondaryReadonlyMasterKey { get; set; }
}