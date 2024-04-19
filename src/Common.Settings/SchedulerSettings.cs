// -----------------------------------------------------------------------
// <copyright file="SchedulerSettings.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Settings;

using System;
using System.ComponentModel.DataAnnotations;

public class SchedulerSettings
{
    [Required]
    public string Schedule { get; set; } = "0 12 * * *"; // start to run at 12:00 PM UTC (5AM PST), every day

    public bool CompensateMissedSchedules { get; set; } = true;

    public TimeSpan SleepInterval { get; set; } = TimeSpan.FromMinutes(5);
}