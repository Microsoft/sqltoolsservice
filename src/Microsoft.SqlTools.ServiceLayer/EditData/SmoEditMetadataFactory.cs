//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.EditData
{
    /// <summary>
    /// Factory that generates metadata using a combination of SMO and SqlClient metadata
    /// </summary>
    public class SmoEditMetadataFactory : IEditMetadataFactory
    {
        /// <summary>
        /// Generates a edit-ready metadata object using SMO
        /// </summary>
        /// <param name="connection">Connection to use for getting metadata</param>
        /// <param name="objectName">Name of the object to return metadata for</param>
        /// <param name="objectType">Type of the object to return metadata for</param>
        /// <returns>Metadata about the object requested</returns>
        public EditTableMetadata GetObjectMetadata(DbConnection connection, string objectName, string objectType)
        {
            // Get a connection to the database for SMO purposes
            SqlConnection sqlConn = connection as SqlConnection;
            if (sqlConn == null)
            {
                // It's not actually a SqlConnection, so let's try a reliable SQL connection
                ReliableSqlConnection reliableConn = connection as ReliableSqlConnection;
                if (reliableConn == null)
                {
                    // If we don't have connection we can use with SMO, just give up on using SMO
                    return null;
                }

                // We have a reliable connection, use the underlying connection
                sqlConn = reliableConn.GetUnderlyingConnection();
            }

            // Connect with SMO and get the metadata for the table
            Server server = new Server(new ServerConnection(sqlConn));
            TableViewTableTypeBase smoResult;
            switch (objectType.ToLowerInvariant())
            {
                case "table":
                    smoResult = server.Databases[sqlConn.Database].Tables[objectName];
                    break;
                case "view":
                    smoResult = server.Databases[sqlConn.Database].Views[objectName];
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(objectType), SR.EditDataUnsupportedObjectType(objectType));
            }
            if (smoResult == null)
            {
                throw new ArgumentOutOfRangeException(nameof(objectName), SR.EditDataObjectMetadataNotFound);
            }

            // Generate the edit column metadata
            List<EditColumnMetadata> editColumns = new List<EditColumnMetadata>();
            for (int i = 0; i < smoResult.Columns.Count; i++)
            {
                Column smoColumn = smoResult.Columns[i];

                // The default value may be escaped
                string defaultValue = smoColumn.DefaultConstraint == null
                    ? null
                    : SqlScriptFormatter.UnwrapLiteral(smoColumn.DefaultConstraint.Text);

                EditColumnMetadata column = new EditColumnMetadata
                {
                    Ordinal = i,
                    DefaultValue = defaultValue,
                    EscapedName = SqlScriptFormatter.FormatIdentifier(smoColumn.Name)
                };
                editColumns.Add(column);
            }

            // If a table is memory optimized it is Hekaton. If it's a view, then it can't be Hekaton
            Table smoTable = smoResult as Table;
            bool isMemoryOptimized = smoTable != null && smoTable.IsMemoryOptimized;

            // Escape the parts of the name
            string[] objectNameParts = {smoResult.Schema, smoResult.Name};
            string escapedMultipartName = SqlScriptFormatter.FormatMultipartIdentifier(objectNameParts);

            return new EditTableMetadata
            {
                Columns = editColumns.ToArray(),
                EscapedMultipartName = escapedMultipartName,
                IsMemoryOptimized = isMemoryOptimized,
            };
        }
    }
}