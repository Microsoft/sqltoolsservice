﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{
    /// <summary>
    /// Container class for a selection range from file
    /// </summary>
    public class SelectionData {
        public int StartLine { get; set; }
        public int StartColumn { get; set; }
        public int EndLine { get; set; }
        public int EndColumn { get; set; }
        public SelectionData(int startLine, int startColumn, int endLine, int endColumn) {
            StartLine = startLine;
            StartColumn = startColumn;
            EndLine = endLine;
            EndColumn = endColumn;
        }
    }
    /// <summary>
    /// Parameters for the query execute request
    /// </summary>
    public class QueryExecuteParams
    {
        /// <summary>
        /// The selection from the document
        /// </summary>
        public SelectionData QuerySelection { get; set; }

        /// <summary>
        /// URI for the editor that is asking for the query execute
        /// </summary>
        public string OwnerUri { get; set; }
    }

    /// <summary>
    /// Parameters for the query execute result
    /// </summary>
    public class QueryExecuteResult
    {
        /// <summary>
        /// Connection error messages. Optional, can be set to null to indicate no errors
        /// </summary>
        public string Messages { get; set; }
    }

    public class QueryExecuteRequest
    {
        public static readonly
            RequestType<QueryExecuteParams, QueryExecuteResult> Type =
            RequestType<QueryExecuteParams, QueryExecuteResult>.Create("query/execute");
    }
}
