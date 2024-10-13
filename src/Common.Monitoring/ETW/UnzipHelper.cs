// -----------------------------------------------------------------------
// <copyright file="UnzipHelper.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.ETW
{
    using System;
    using System.IO;
    using System.IO.Compression;

    public class UnzipHelper
    {
        private readonly string zipFile;
        private readonly string outputFolder;
        private readonly string ext;

        public UnzipHelper(string zipFile, string outputFolder, string ext)
        {
            this.zipFile = zipFile;
            this.outputFolder = outputFolder;
            this.ext = ext;
        }

        public void Process()
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            this.ExtractZipFile(this.zipFile, tempFolder);
            var files = Directory.GetFiles(tempFolder, $"*.{this.ext}", SearchOption.AllDirectories);
            foreach (var filePath in files)
            {
                File.Move(filePath, Path.Combine(this.outputFolder, Path.GetFileName(filePath)), true);
            }
            Directory.Delete(tempFolder, true);
        }

        private void ExtractZipFile(string zipFilePath, string extractionPath)
        {
            // Create the extraction directory if it doesn't exist
            if (!Directory.Exists(extractionPath))
            {
                Directory.CreateDirectory(extractionPath);
            }

            // Open the zip file
            using var archive = ZipFile.OpenRead(zipFilePath);
            foreach (var entry in archive.Entries)
            {
                var destinationPath = Path.Combine(extractionPath, entry.FullName);

                // Normalize the directory structure (convert '/' to system directory separator)
                destinationPath = destinationPath.Replace("/", Path.DirectorySeparatorChar.ToString());

                // Check if it's a directory or file
                if (string.IsNullOrEmpty(entry.Name)) // It's a directory
                {
                    // Create the directory if it doesn't exist
                    if (!Directory.Exists(destinationPath))
                    {
                        Directory.CreateDirectory(destinationPath);
                    }
                }
                else if (entry.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) // Nested ZIP file
                {
                    // Extract the nested ZIP file into a subdirectory
                    var nestedZipExtractionPath =
                        Path.Combine(extractionPath, Path.GetFileNameWithoutExtension(entry.Name));

                    // Ensure directory for nested zip extraction exists
                    if (!Directory.Exists(nestedZipExtractionPath))
                    {
                        Directory.CreateDirectory(nestedZipExtractionPath);
                    }

                    // Copy the nested ZIP file to a temporary location
                    var tempZipPath = Path.Combine(nestedZipExtractionPath, entry.Name);
                    entry.ExtractToFile(tempZipPath, overwrite: true);

                    // Recursively extract the nested ZIP file
                    this.ExtractZipFile(tempZipPath, nestedZipExtractionPath);

                    // Optionally, delete the extracted nested ZIP file after processing
                    File.Delete(tempZipPath);
                }
                else // It's a file
                {
                    // Ensure the directory for the file exists
                    var directoryPath = Path.GetDirectoryName(destinationPath);
                    if (directoryPath != null && !Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    // Copy the file to the target location
                    entry.ExtractToFile(destinationPath, overwrite: true);
                }
            }
        }
    }
}