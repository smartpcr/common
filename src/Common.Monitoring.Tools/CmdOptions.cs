// -----------------------------------------------------------------------
// <copyright file="CmdOptions.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Tools
{
    using CommandLine;

    public class CmdOptions
    {
        [Option('i', "InputFolder", Required = true, HelpText = "Path to the input folder.")]
        public string InputFolder { get; set; }

        [Option('o', "OutputFolder", Required = true, HelpText = "Path to the output folder.")]
        public string OutputFolder { get; set; }
    }
}