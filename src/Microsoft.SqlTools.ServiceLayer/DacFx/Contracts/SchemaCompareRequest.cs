﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Utility;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    public enum SchemaCompareEndpointType
    {
        database,
        dacpac
    }

    public class SchemaCompareEndpointInfo
    {
        /// <summary>
        /// Gets or sets the type of the endpoint
        /// </summary>
        public SchemaCompareEndpointType EndpointType { get; set; }

        /// <summary>
        /// Gets or sets package filepath
        /// </summary>
        public string PackageFilePath { get; set; }

        /// <summary>
        /// Gets or sets name for the database
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Connection uri
        /// </summary>
        public string OwnerUri { get; set; }
    }

    /// <summary>
    /// Parameters for a schema compare request.
    /// </summary>
    public class SchemaCompareParams
    {
        /// <summary>
        /// Gets or sets the source endpoint info
        /// </summary>
        public SchemaCompareEndpointInfo sourceEndpointInfo { get; set; }

        /// <summary>
        /// Gets or sets the target endpoint info
        /// </summary>
        public SchemaCompareEndpointInfo targetEndpointInfo { get; set; }

        /// <summary>
        /// Executation mode for the operation. Default is execution
        /// </summary>
        public TaskExecutionMode TaskExecutionMode { get; set; }
    }

    /// <summary>
    /// Parameters returned from a schema compare request.
    /// </summary>
    public class SchemaCompareResult : DacFxResult
    {
        public bool AreEqual { get; set; }

        public List<DiffEntry> Differences { get; set; }
    }

    public class DiffEntry
    {
        public SchemaUpdateAction updateAction;
        public SchemaDifferenceType differenceType;
        public string name;
        public string sourceValue;
        public string targetValue;
        public DiffEntry parent;
        public List<DiffEntry> children;
        public string sourceScript;
        public string targetScript;
    }


    /// <summary>
    /// Defines the Schema Compare request type
    /// </summary>
    class SchemaCompareRequest
    {
        public static readonly RequestType<SchemaCompareParams, SchemaCompareResult> Type =
            RequestType<SchemaCompareParams, SchemaCompareResult>.Create("dacfx/schemaCompare");
    }
}
