// -----------------------------------------------------------------------
// <copyright file="RedisGroup.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Settings;

using System.Collections.Generic;

public class RedisGroup : Dictionary<string, RedisConnectionSettings>
{
}