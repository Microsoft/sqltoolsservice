﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.Test.Utility;
using Xunit;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Connection
{
    /// <summary>
    /// Tests for the ServiceHost Connection Service tests that require a live database connection
    /// </summary>
    public class ConnectionServiceTests
    {
        [Fact]
        public void RunningMultipleQueriesCreatesOnlyOneConnection()
        {
            // Connect/disconnect twice to ensure reconnection can occur
            ConnectionService service = ConnectionService.Instance;
            service.OwnerToConnectionMap.Clear();
            for (int i = 0; i < 2; i++)
            {
                var result = TestObjects.InitLiveConnectionInfo();
                ConnectionInfo connectionInfo = result.ConnectionInfo;
                string uri = connectionInfo.OwnerUri;

                // We should see one ConnectionInfo and one DbConnection
                Assert.Equal(1, connectionInfo.CountConnections);
                Assert.Equal(1, service.OwnerToConnectionMap.Count);

                // If we run a query
                var fileStreamFactory = Common.GetFileStreamFactory(new Dictionary<string, byte[]>());
                Query query = new Query(Common.StandardQuery, connectionInfo, new QueryExecutionSettings(), fileStreamFactory);
                query.Execute();
                query.ExecutionTask.Wait();

                // We should see two DbConnections
                Assert.Equal(2, connectionInfo.CountConnections);

                // If we run another query
                query = new Query(Common.StandardQuery, connectionInfo, new QueryExecutionSettings(), fileStreamFactory);
                query.Execute();
                query.ExecutionTask.Wait();

                // We should still have 2 DbConnections
                Assert.Equal(2, connectionInfo.CountConnections);

                // If we disconnect, we should remain in a consistent state to do it over again
                // e.g. loop and do it over again
                service.Disconnect(new DisconnectParams() { OwnerUri = connectionInfo.OwnerUri });

                // We should be left with an empty connection map
                Assert.Equal(0, service.OwnerToConnectionMap.Count);
            }
        }

        [Fact]
        public void DatabaseChangesAffectAllConnections()
        {
            // If we make a connection to a live database 
            ConnectionService service = ConnectionService.Instance;
            var result = TestObjects.InitLiveConnectionInfo();
            ConnectionInfo connectionInfo = result.ConnectionInfo;
            ConnectionDetails details = connectionInfo.ConnectionDetails;
            string uri = connectionInfo.OwnerUri;
            string initialDatabaseName = details.DatabaseName;
            string newDatabaseName = "tempdb";
            string changeDatabaseQuery = "use " + newDatabaseName;

            // Then run any query to create a query DbConnection
            var fileStreamFactory = Common.GetFileStreamFactory(new Dictionary<string, byte[]>());
            Query query = new Query(Common.StandardQuery, connectionInfo, new QueryExecutionSettings(), fileStreamFactory);
            query.Execute();
            query.ExecutionTask.Wait();

            // All open DbConnections (Query and Default) should have initialDatabaseName as their database
            foreach (DbConnection connection in connectionInfo.AllConnections)
            {
                Assert.Equal(connection.Database, initialDatabaseName);
            }

            // If we run a query to change the database
            query = new Query(changeDatabaseQuery, connectionInfo, new QueryExecutionSettings(), fileStreamFactory);
            query.Execute();
            query.ExecutionTask.Wait();

            // All open DbConnections (Query and Default) should have newDatabaseName as their database
            foreach (DbConnection connection in connectionInfo.AllConnections)
            {
                Assert.Equal(connection.Database, newDatabaseName);
            }
        }
    }
}
