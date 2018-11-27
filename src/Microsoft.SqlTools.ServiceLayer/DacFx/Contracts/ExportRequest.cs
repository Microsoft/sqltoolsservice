﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    /// <summary>
    /// Parameters for a DacFx export request.
    /// </summary>
    public class ExportParams : IDacFxParams
    {
        /// <summary>
        /// Gets or sets package filepath
        /// </summary>
        public string PackageFilePath { get; set; }

        /// <summary>
        /// Gets or sets name for database
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Connection uri
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Executation mode for the operation. Default is execution
        /// </summary>
        public TaskExecutionMode TaskExecutionMode { get; set; }
    }

    /// <summary>
    /// Defines the DacFx export request type
    /// </summary>
    class ExportRequest
    {
        public static readonly RequestType<ExportParams, DacFxResult> Type =
            RequestType<ExportParams, DacFxResult>.Create("dacfx/export");
    }
}
