﻿// Copyright 2014 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.WindowsAzure.Storage;
using Serilog.Core;
using Serilog.Events;
using System;
using System.IO;
using System.Threading;
using Serilog.Sinks.AzureBlobStorage.AzureBlobProvider;

namespace Serilog.Sinks.AzureBlobStorage
{
    /// <summary>
    /// Writes log events as records to an Azure Blob Storage blob.
    /// </summary>
    public class AzureBlobStorageWithPropertiesSink : ILogEventSink
    {        
        private readonly IFormatProvider formatProvider;
        private readonly CloudStorageAccount storageAccount;
        private readonly string storageFolderName;
        private readonly string storageFileName;
        private readonly bool bypassFolderCreationValidation;
        private readonly ICloudBlobProvider cloudBlobProvider;

        /// <summary>
        /// Construct a sink that saves logs to the specified storage account.
        /// </summary>
        /// <param name="storageAccount">The Cloud Storage Account to use to insert the log entries to.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="storageFolderName">Container name that log entries will be written to.</param>
        /// <param name="storageFileName">File name that log entries will be written to.</param>
        /// <param name="bypassFolderCreationValidation">Bypass the exception in case the blob creation fails.</param>
        /// <param name="cloudBlobProvider">Cloud blob provider to get current log blob.</param>
        public AzureBlobStorageWithPropertiesSink(CloudStorageAccount storageAccount,
            IFormatProvider formatProvider,
            string storageFolderName = null,
            string storageFileName = null,
            bool bypassFolderCreationValidation = false,
            ICloudBlobProvider cloudBlobProvider = null)
        {
            if (string.IsNullOrEmpty(storageFolderName))
            {
                storageFolderName = "logging";
            }

            if (string.IsNullOrEmpty(storageFileName))
            {
                storageFileName = "log.txt";
            }

            this.storageAccount = storageAccount;
            this.storageFolderName = storageFolderName;
            this.storageFileName = storageFileName;
            this.bypassFolderCreationValidation = bypassFolderCreationValidation;
            this.cloudBlobProvider = cloudBlobProvider ?? new DefaultCloudBlobProvider();

            this.formatProvider = formatProvider;                     
        }

        /// <summary>
        /// Emit the provided log event to the sink.
        /// </summary>
        /// <param name="logEvent">The log event to write.</param>
        public void Emit(LogEvent logEvent)
        {
            var blob = cloudBlobProvider.GetCloudBlob(storageAccount, storageFolderName, storageFileName, bypassFolderCreationValidation);            

            using (MemoryStream stream = new MemoryStream())
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write(logEvent.RenderMessage() + Environment.NewLine);
                    writer.Flush();
                    stream.Position = 0;

                    blob.AppendBlockAsync(stream).ConfigureAwait(false);
                }
            }
        }
    }
}
