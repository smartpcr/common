// -----------------------------------------------------------------------
// <copyright file="RollingFileLogger.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Monitoring.Sinks;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class RollingFileLogger : IDisposable, IAsyncDisposable
{
    private readonly string _filePrefix;
    private readonly string _fileExtension;
    private readonly string _currentDirectory;
    private readonly long _maxFileSize;
    private readonly int _maxFileCount;
    private readonly int _maxRetentionInDays;
    private readonly object _lock = new object();
    private StreamWriter _currentWriter;
    private int _fileIndex;
    private DateTime _currentDate;

    public RollingFileLogger(FileSinkSettings fileSink)
    {
        _filePrefix = fileSink.FilePrefix;
        _fileExtension = fileSink.FileExtension ?? "log";
        _maxFileCount = fileSink.MaxFileCount;
        _maxRetentionInDays = fileSink.MaxFileRetentionInDays;
        _fileIndex = 0;
        _currentDate = DateTime.UtcNow.Date;

        _maxFileSize = fileSink.MaxFileSizeMb * 1024 * 1024;
        _currentDirectory = string.IsNullOrEmpty(fileSink.MonitoringFolder) ? Directory.GetCurrentDirectory() : fileSink.MonitoringFolder;
        if (!Directory.Exists(_currentDirectory))
        {
            Directory.CreateDirectory(_currentDirectory);
        }

        _currentWriter = CreateWriter();
    }

    public void Log(string message)
    {
        lock (_lock)
        {
            if (DateTime.UtcNow.Date != _currentDate)
            {
                _currentWriter.Dispose();
                _currentDate = DateTime.UtcNow.Date;
                _fileIndex = 0;
                PurgeOldFiles();
                _currentWriter = CreateWriter();
            }

            _currentWriter.WriteLine(message);
            _currentWriter.Flush();

            if (_currentWriter.BaseStream.Length >= _maxFileSize)
            {
                _currentWriter.Dispose();
                _currentWriter = CreateWriter();
            }
        }
    }

    private StreamWriter CreateWriter()
    {
        string logFilePath = Path.Combine(_currentDirectory, $"{_filePrefix}_{_currentDate:yyyy_MM_dd}_{_fileIndex:000}.{_fileExtension}");
        var fileInfo = new FileInfo(logFilePath);
        if (!fileInfo.Exists)
        {
            return File.AppendText(logFilePath);
        }

        if (fileInfo.Length < _maxFileSize)
        {
            return File.AppendText(logFilePath);
        }

        while (fileInfo.Exists && fileInfo.Length >= _maxFileSize)
        {
            _fileIndex++;
            logFilePath = Path.Combine(_currentDirectory, $"{_filePrefix}_{_currentDate:yyyy_MM_dd}_{_fileIndex:000}.{_fileExtension}");
            fileInfo = new FileInfo(logFilePath);
        }

        return File.AppendText(logFilePath);
    }

    private void PurgeOldFiles()
    {
        var files = Directory.GetFiles(_currentDirectory, $"*.{_fileExtension}");
        if (files.Length > _maxFileCount)
        {
            // Sort by creation time, remove oldest files until files count is less than max file count
            files.OrderBy(File.GetCreationTimeUtc)
                .Take(files.Length - _maxFileCount)
                .ToList()
                .ForEach(File.Delete);
            files = Directory.GetFiles(_currentDirectory, $"*.{_fileExtension}");
        }

        foreach (var file in files)
        {
            var creationTime = File.GetCreationTimeUtc(file);
            if (creationTime.AddDays(_maxRetentionInDays) < DateTime.UtcNow)
            {
                File.Delete(file);
            }
        }
    }

    public void Dispose() => _currentWriter.Dispose();

    public async ValueTask DisposeAsync() => await _currentWriter.DisposeAsync();
}