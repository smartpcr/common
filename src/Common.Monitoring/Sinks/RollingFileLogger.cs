// -----------------------------------------------------------------------
// <copyright file="RollingFileLogger.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Sinks;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

public class RollingFileLogger
{
    private readonly string filePrefix;
    private readonly string fileExtension;
    private readonly string currentDirectory;
    private readonly long maxFileSize;
    private readonly int maxEntriesInFile;
    private readonly int maxFileCount;
    private readonly int maxRetentionInDays;
    private readonly SemaphoreSlim fileLock = new SemaphoreSlim(1, 1);
    private string currentLogFile;
    private int fileIndex;
    private DateTime currentDate;
    private int totalLogEntries;

    public RollingFileLogger(FileSinkSettings fileSink, string defaultFilePrefix)
    {
        this.filePrefix = fileSink.FilePrefix ?? defaultFilePrefix;
        this.fileExtension = fileSink.FileExtension ?? "log";
        this.maxFileCount = fileSink.MaxFileCount;
        this.maxRetentionInDays = fileSink.MaxFileRetentionInDays;
        this.fileIndex = 0;
        this.totalLogEntries = 0;
        this.currentDate = DateTime.UtcNow.Date;

        this.maxFileSize = fileSink.MaxFileSizeMb * 1024 * 1024;
        this.maxEntriesInFile = fileSink.MaxEntriesInFile;
        this.currentDirectory = string.IsNullOrEmpty(fileSink.MonitoringFolder) ? Directory.GetCurrentDirectory() : fileSink.MonitoringFolder;
        if (!Directory.Exists(this.currentDirectory))
        {
            Directory.CreateDirectory(this.currentDirectory);
        }

        this.currentLogFile = this.CreateWriter();
    }

    public void Log(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        this.fileLock.Wait();

        try
        {
            if (DateTime.UtcNow.Date != this.currentDate)
            {
                this.currentDate = DateTime.UtcNow.Date;
                this.fileIndex = 0;
                this.PurgeOldFiles();
                this.currentLogFile = this.CreateWriter();
                this.totalLogEntries = 0;
            }

            File.AppendAllLines(this.currentLogFile, new[] { message });
            this.totalLogEntries++;

            if (this.totalLogEntries > this.maxEntriesInFile && new FileInfo(this.currentLogFile).Length > this.maxFileSize)
            {
                this.currentLogFile = this.CreateWriter();
            }
        }
        finally
        {
            this.fileLock.Release();
        }
    }

    public void Log(List<string>? messages)
    {
        if (messages == null || !messages.Any())
        {
            return;
        }

        this.fileLock.Wait();
        try
        {
            foreach (var message in messages)
            {
                if (DateTime.UtcNow.Date != this.currentDate)
                {
                    this.currentDate = DateTime.UtcNow.Date;
                    this.fileIndex = 0;
                    this.PurgeOldFiles();
                    this.currentLogFile = this.CreateWriter();
                    this.totalLogEntries = 0;
                }

                File.AppendAllLines(this.currentLogFile, new[] { message });
                this.totalLogEntries++;

                if (this.totalLogEntries > this.maxEntriesInFile && new FileInfo(this.currentLogFile).Length > this.maxFileSize)
                {
                    this.currentLogFile = this.CreateWriter();
                }
            }
        }
        finally
        {
            this.fileLock.Release();
        }
    }

    private string CreateWriter()
    {
        var logFilePath = Path.Combine(this.currentDirectory, $"{this.filePrefix}_{this.currentDate:yyyy_MM_dd}_{this.fileIndex:000}.{this.fileExtension}");
        var fileInfo = new FileInfo(logFilePath);
        if (!fileInfo.Exists)
        {
            // create empty file
            File.WriteAllText(logFilePath, string.Empty);
            return logFilePath;
        }

        if (fileInfo.Length < this.maxFileSize)
        {
            return logFilePath;
        }

        while (fileInfo.Exists && fileInfo.Length >= this.maxFileSize)
        {
            this.fileIndex++;
            logFilePath = Path.Combine(this.currentDirectory, $"{this.filePrefix}_{this.currentDate:yyyy_MM_dd}_{this.fileIndex:000}.{this.fileExtension}");
            fileInfo = new FileInfo(logFilePath);
        }

        File.WriteAllText(logFilePath, string.Empty);
        return logFilePath;
    }

    private void PurgeOldFiles()
    {
        var files = Directory.GetFiles(this.currentDirectory, $"*.{this.fileExtension}");
        if (files.Length > this.maxFileCount)
        {
            // Sort by creation time, remove the oldest file until files count is less than max file count
            files.OrderBy(File.GetCreationTimeUtc)
                .Take(files.Length - this.maxFileCount)
                .ToList()
                .ForEach(File.Delete);
            files = Directory.GetFiles(this.currentDirectory, $"*.{this.fileExtension}");
        }

        foreach (var file in files)
        {
            var creationTime = File.GetCreationTimeUtc(file);
            if (creationTime.AddDays(this.maxRetentionInDays) < DateTime.UtcNow)
            {
                File.Delete(file);
            }
        }
    }
}