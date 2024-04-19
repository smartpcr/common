// -----------------------------------------------------------------------
// <copyright file="HostEnvironment.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Config;

public interface IHostEnvironment
{
    string EnvName { get; set; }
    bool IsDevelopment { get; }
}

public class HostEnvironment : IHostEnvironment
{
    public string EnvName { get; set; }
    public bool IsDevelopment => EnvName == "Development";

    public HostEnvironment(string envName)
    {
        EnvName = envName;
    }
}