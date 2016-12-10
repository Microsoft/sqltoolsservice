//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.Common;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Connection
{
    /// <summary>
    /// Information pertaining to a unique connection instance.
    /// </summary>
    public class ConnectionInfo
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ConnectionInfo(ISqlConnectionFactory factory, string ownerUri, ConnectionDetails details)
        {
            Factory = factory;
            OwnerUri = ownerUri;
            ConnectionDetails = details;
            ConnectionId = Guid.NewGuid();
            IntellisenseMetrics = new InteractionMetrics<double>(new int[] { 50, 100, 200, 500, 1000, 2000 });
        }

        /// <summary>
        /// Unique Id, helpful to identify a connection info object
        /// </summary>
        public Guid ConnectionId { get; private set; }

        /// <summary>
        /// URI identifying the owner/user of the connection. Could be a file, service, resource, etc.
        /// </summary>
        public string OwnerUri { get; private set; }

        /// <summary>
        /// Factory used for creating the SQL connection associated with the connection info.
        /// </summary>
        public ISqlConnectionFactory Factory {get; private set;}

        /// <summary>
        /// Properties used for creating/opening the SQL connection.
        /// </summary>
        public ConnectionDetails ConnectionDetails { get; private set; }
        
        /// <summary>
        /// The connection to the SQL database that commands will be run against.
        /// </summary>
        public DbConnection SqlConnection { get; set; }

        /// <summary>
        /// The connection to the SQL database that queries will be run against.
        /// A single instance is created and re-used after the first query is run 
        /// </summary>
        public DbConnection QueryConnection { get; set; }

        /// <summary>
        /// Intellisense Metrics
        /// </summary>
        public InteractionMetrics<double> IntellisenseMetrics { get; private set; }

        /// <summary>
        /// Returns true is the db connection is to a SQL db
        /// </summary>
        public bool IsAzure { get; set; }
    }
}
