﻿// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using Microsoft.Extensions.Logging;

namespace Monai.Deploy.InformaticsGateway.Logging
{
    public static partial class Log
    {
        [LoggerMessage(EventId = 4000, Level = LogLevel.Information, Message = "Temporary Storage Path={path}.")]
        public static partial void TemporaryStoragePath(this ILogger logger, string path);

        [LoggerMessage(EventId = 4001, Level = LogLevel.Information, Message = "Storage Size: {totalSize:N0}. Reserved: {reservedSpace:N0}.")]
        public static partial void StorageSizeWithReserve(this ILogger logger, long totalSize, long reservedSpace);

        [LoggerMessage(EventId = 4002, Level = LogLevel.Information, Message = "Storage Size: {totalSize:N0}. Reserved: {reservedSpace:N0}. Available: {freeSpace:N0}.")]
        public static partial void StorageSizeWithReserveAndAvailable(this ILogger logger, long totalSize, long reservedSpace, long freeSpace);

        [LoggerMessage(EventId = 4003, Level = LogLevel.Debug, Message = "Waiting for instance...")]
        public static partial void SpaceReclaimerWaitingForTask(this ILogger logger);

        [LoggerMessage(EventId = 4004, Level = LogLevel.Error, Message = "Error occurred deleting file {file} on {retryCount} retry.")]
        public static partial void ErrorDeletingFIle(this ILogger logger, string file, int retryCount, Exception ex);

        [LoggerMessage(EventId = 4005, Level = LogLevel.Debug, Message = "Deleting file {filePath}.")]
        public static partial void DeletingFile(this ILogger logger, string filePath);

        [LoggerMessage(EventId = 4006, Level = LogLevel.Debug, Message = "File deleted {filePath}.")]
        public static partial void FileDeleted(this ILogger logger, string filePath);

        [LoggerMessage(EventId = 4007, Level = LogLevel.Debug, Message = "Deleting directory {directory}.")]
        public static partial void DeletingDirectory(this ILogger logger, string directory);

        [LoggerMessage(EventId = 4008, Level = LogLevel.Error, Message = "Error deleting directory {directory}.")]
        public static partial void ErrorDeletingDirectory(this ILogger logger, string directory, Exception ex);

        [LoggerMessage(EventId = 4009, Level = LogLevel.Debug, Message = "File added to cleanup queue {file}. Queue size: {size}.")]
        public static partial void InstanceAddedToCleanupQueue(this ILogger logger, string file, int size);
    }
}
