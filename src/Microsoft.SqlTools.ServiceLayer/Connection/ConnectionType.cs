﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.Connection
{
    /// <summary>
    /// Categorizations of DbConnection instances. 
    /// 
    /// Default: Connection used by the editor. Opened by the editor upon the initial connection. 
    /// Query: Connection used for executing queries. Opened when the first query is executed.
    /// </summary>
    public enum ConnectionType
    {
        Default = 1,
        Query
    }
}
