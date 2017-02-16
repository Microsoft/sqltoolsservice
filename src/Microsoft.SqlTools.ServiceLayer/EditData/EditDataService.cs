﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.EditData.Contracts;
using Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
using Microsoft.SqlTools.ServiceLayer.Utility;
using ConnectionType = Microsoft.SqlTools.ServiceLayer.Connection.ConnectionType;

namespace Microsoft.SqlTools.ServiceLayer.EditData
{
    public class EditDataService : IDisposable
    {
        #region Singleton Instance Implementation

        private static readonly Lazy<EditDataService> LazyInstance = new Lazy<EditDataService>(() => new EditDataService());

        public static EditDataService Instance => LazyInstance.Value;

        private EditDataService()
        {
            queryExecutionService = QueryExecutionService.Instance;
            connectionService = ConnectionService.Instance;
        }

        internal EditDataService(QueryExecutionService qes, ConnectionService cs)
        {
            queryExecutionService = qes;
            connectionService = cs;
        }

        #endregion

        #region Member Variables 

        private readonly ConnectionService connectionService;

        private readonly QueryExecutionService queryExecutionService;

        private readonly Lazy<ConcurrentDictionary<string, Session>> editSessions = new Lazy<ConcurrentDictionary<string, Session>>(
            () => new ConcurrentDictionary<string, Session>());

        #endregion

        #region Properties

        /// <summary>
        /// Dictionary mapping OwnerURIs to active sessions
        /// </summary>
        internal ConcurrentDictionary<string, Session> ActiveSessions => editSessions.Value;

        #endregion

        /// <summary>
        /// Initializes the edit data service with the service host
        /// </summary>
        /// <param name="serviceHost">The service host to register commands/events with</param>
        public void InitializeService(ServiceHost serviceHost)
        {
            // Register handlers for requests
            serviceHost.SetRequestHandler(EditCreateRowRequest.Type, HandleCreateRowRequest);
            serviceHost.SetRequestHandler(EditDeleteRowRequest.Type, HandleDeleteRowRequest);
            serviceHost.SetRequestHandler(EditDisposeRequest.Type, HandleDisposeRequest);
            serviceHost.SetRequestHandler(EditInitializeRequest.Type, HandleInitializeRequest);
            serviceHost.SetRequestHandler(EditRevertRowRequest.Type, HandleRevertRowRequest);
            serviceHost.SetRequestHandler(EditUpdateCellRequest.Type, HandleUpdateCellRequest);

            // Register handler for shutdown event
            serviceHost.RegisterShutdownTask((shutdownParams, requestContext) =>
            {
                Dispose();
                return Task.FromResult(0);
            });
        }

        #region Request Handlers

        internal async Task HandleCreateRowRequest(EditCreateRowParams createParams,
            RequestContext<EditCreateRowResult> requestContext)
        {
            try
            {
                Session session = GetActiveSessionOrThrow(createParams.OwnerUri);

                // Create the row and get send the ID of the row back
                long newRowId = session.CreateRow();
                EditCreateRowResult createResult = new EditCreateRowResult
                {
                    NewRowId = newRowId
                };
                await requestContext.SendResult(createResult);
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.Message);
            }
        }

        internal async Task HandleDeleteRowRequest(EditDeleteRowParams deleteParams,
            RequestContext<EditDeleteRowResult> requestContext)
        {
            try
            {
                Session session = GetActiveSessionOrThrow(deleteParams.OwnerUri);

                // Add the delete row to the edit cache
                session.DeleteRow(deleteParams.RowId);
                await requestContext.SendResult(new EditDeleteRowResult());
            }
            catch (Exception e)
            {
                // Send back the error
                await requestContext.SendError(e.Message);
            }
        }

        internal async Task HandleDisposeRequest(EditDisposeParams disposeParams,
            RequestContext<EditDisposeResult> requestContext)
        {
            try
            {
                // Sanity check the owner URI
                Validate.IsNotNullOrWhitespaceString(nameof(disposeParams.OwnerUri), disposeParams.OwnerUri);

                // Attempt to remove the session
                Session session;
                if (!ActiveSessions.TryRemove(disposeParams.OwnerUri, out session))
                {
                    // @TODO: Move to constants file
                    await requestContext.SendError(SR.EditDataSessionNotFound);
                    return;
                }

                // Everything was successful, return success
                await requestContext.SendResult(new EditDisposeResult());
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.Message);
            }
        }

        internal async Task HandleInitializeRequest(EditInitializeParams initParams,
            RequestContext<EditInitializeResult> requestContext)
        {
            try
            {          
                // Make sure we have info to process this request
                Validate.IsNotNullOrWhitespaceString(nameof(initParams.OwnerUri), initParams.OwnerUri);
                Validate.IsNotNullOrWhitespaceString(nameof(initParams.ObjectName), initParams.ObjectName);

                // Setup a callback for when the query has successfully created
                Func<Query, Task<bool>> queryCreateSuccessCallback = async query =>
                {
                    await requestContext.SendResult(new EditInitializeResult());
                    return true;
                };

                // Setup a callback for when the query failed to be created
                Func<string, Task> queryCreateFailureCallback = requestContext.SendError;

                // Setup a callback for when the query completes execution successfully
                Query.QueryAsyncEventHandler queryCompleteSuccessCallback = async query =>
                {
                    EditSessionReadyParams readyParams = new EditSessionReadyParams
                    {
                        OwnerUri = initParams.OwnerUri
                    };

                    try
                    {
                        // Get a connection we'll use for SMO metadata lookup (and committing, later on)
                        // @TODO: Replace with factory pattern!
                        var smoMetadata = await GetSmoMetadata(initParams.OwnerUri, initParams.ObjectName, initParams.ObjectType);
                        var metadata = new EditTableMetadata(query.Batches[0].ResultSets[0].Columns, smoMetadata);

                        // Create the session and add it to the sessions list
                        Session session = new Session(query, metadata);
                        if (!ActiveSessions.TryAdd(initParams.OwnerUri, session))
                        {
                            throw new InvalidOperationException("Failed to create edit session, session already exists.");
                        }
                        readyParams.Success = true;
                    }
                    catch (Exception)
                    {
                        // Request that the query be disposed
                        await queryExecutionService.InterServiceDisposeQuery(initParams.OwnerUri, null, null);
                        readyParams.Success = false;
                    }

                    // Send the edit session ready notification
                    await requestContext.SendEvent(EditSessionReadyEvent.Type, readyParams);
                };

                // Setup a callback for when the query completes execution with failure
                Query.QueryAsyncEventHandler queryCompleteFailureCallback = query =>
                {
                    EditSessionReadyParams readyParams = new EditSessionReadyParams
                    {
                        OwnerUri = initParams.OwnerUri,
                        Success = false
                    };
                    return requestContext.SendEvent(EditSessionReadyEvent.Type, readyParams);
                };

                // Put together a query for the results and execute it
                ExecuteStringParams executeParams = new ExecuteStringParams
                {
                    Query = $"SELECT * FROM {SqlScriptFormatter.FormatMultipartIdentifier(initParams.ObjectName)}",
                    OwnerUri = initParams.OwnerUri
                };
                await queryExecutionService.InterServiceExecuteQuery(executeParams, requestContext,
                    queryCreateSuccessCallback, queryCreateFailureCallback,
                    queryCompleteSuccessCallback, queryCompleteFailureCallback);
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.Message);
            }
        }

        internal async Task HandleRevertRowRequest(EditRevertRowParams revertParams,
            RequestContext<EditRevertRowResult> requestContext)
        {
            try
            {
                Session session = GetActiveSessionOrThrow(revertParams.OwnerUri);
                session.RevertRow(revertParams.RowId);
                await requestContext.SendResult(new EditRevertRowResult());
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.Message);
            }
        }

        internal async Task HandleUpdateCellRequest(EditUpdateCellParams updateParams,
            RequestContext<EditUpdateCellResult> requestContext)
        {
            try
            {
                Session session = GetActiveSessionOrThrow(updateParams.OwnerUri);
                var result = session.UpdateCell(updateParams.RowId, updateParams.ColumnId, updateParams.NewValue);
                await requestContext.SendResult(result);
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.Message);
            }
        }

        #endregion

        #region Private Helpers

        private async Task<TableViewTableTypeBase> GetSmoMetadata(string ownerUri, string objectName, string objectType)
        {
            // Get a connection to the database for edit purposes
            DbConnection conn = await connectionService.GetOrOpenConnection(ownerUri, ConnectionType.Edit);
            ReliableSqlConnection reliableConn = conn as ReliableSqlConnection;
            if (reliableConn == null)
            {
                // If we don't have connection we can use with SMO, just give up on using SMO
                return null;
            }

            SqlConnection sqlConn = reliableConn.GetUnderlyingConnection();
            Server server = new Server(new ServerConnection(sqlConn));
            TableViewTableTypeBase result;
            switch (objectType.ToLowerInvariant())
            {
                case "table":
                    result = server.Databases[sqlConn.Database].Tables["identitytest"];
                    break;
                case "view":
                    result = server.Databases[sqlConn.Database].Views[objectName];
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(objectType), SR.EditDataUnsupportObjectType(objectType));
            }
            if (result == null)
            {
                throw new ArgumentOutOfRangeException(nameof(objectName), SR.EditDataObjectMetadataNotFound);
            }

            return result;
        }

        /// <summary>
        /// Returns the session with the given owner URI or throws if it can't be found
        /// </summary>
        /// <exception cref="Exception">If the edit session doesn't exist</exception>
        /// <param name="ownerUri">Owner URI for the edit session</param>
        /// <returns>The edit session that corresponds to the owner URI</returns>
        private Session GetActiveSessionOrThrow(string ownerUri)
        {
            // Sanity check the owner URI is provided
            Validate.IsNotNullOrWhitespaceString(nameof(ownerUri), ownerUri);

            // Attempt to get the session, throw if unable
            Session session;
            if (!ActiveSessions.TryGetValue(ownerUri, out session))
            {
                throw new Exception(SR.EditDataSessionNotFound);
            }

            return session;
        }

        #endregion

        #region IDisposable Implementation

        private bool disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                // TODO: Dispose objects that need disposing
            }

            disposed = true;
        }

        ~EditDataService()
        {
            Dispose(false);
        }

        #endregion

    }
}
