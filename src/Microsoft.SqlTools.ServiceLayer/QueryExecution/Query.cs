﻿// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Utility;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Common; 

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution
{
    /// <summary>
    /// Internal representation of an active query
    /// </summary>
    public class Query : IDisposable
    {
        /// <summary>
        /// "Error" code produced by SQL Server when the database context (name) for a connection changes.
        /// </summary>
        private const int DatabaseContextChangeErrorNumber = 5701;

        #region Member Variables

        /// <summary>
        /// Cancellation token source, used for cancelling async db actions
        /// </summary>
        private readonly CancellationTokenSource cancellationSource;

        /// <summary>
        /// For IDisposable implementation, whether or not this object has been disposed
        /// </summary>
        private bool disposed;

        /// <summary>
        /// The connection info associated with the file editor owner URI, used to create a new
        /// connection upon execution of the query
        /// </summary>
        private readonly ConnectionInfo editorConnection;

        /// <summary>
        /// Whether or not the execute method has been called for this query
        /// </summary>
        private bool hasExecuteBeenCalled;

        /// <summary>
        /// ON keyword
        /// </summary>
        private static string s_On = "ON";

        /// <summary>
        /// OFF keyword
        /// </summary>
        private static string s_Off = "OFF";
 
        /// <summary>
        /// showplan_text statement
        /// </summary>
        private static string s_SetShowplanText = "SET SHOWPLAN_TEXT {0}";

        /// <summary>
        /// statistics time statement
        /// </summary>
        private static string s_SetStatisticsTime = "SET STATISTICS TIME {0}";

        /// <summary>
        /// statistics IO statement
        /// </summary>
        private static string s_SetStatisticsIO = "SET STATISTICS IO {0}";

        /// <summary>
        /// statistics profile statement
        /// </summary>
        private static string s_SetStatisticsProfile = "SET STATISTICS PROFILE {0}";
 
        /// <summary>
        /// showplan_all statement
        /// </summary>
		private static string s_SetShowPlanAll = "SET SHOWPLAN_ALL {0}";

        /// <summary>
        /// showplan_xml statement
        /// </summary>
		private static string s_SetShowPlanXml = "SET SHOWPLAN_XML {0}";

        /// <summary>
        /// statistics_all statement
        /// </summary>
        private static string s_SetStatisticsXml = "SET STATISTICS XML {0}";

        #endregion

        /// <summary>
        /// Constructor for a query
        /// </summary>
        /// <param name="queryText">The text of the query to execute</param>
        /// <param name="connection">The information of the connection to use to execute the query</param>
        /// <param name="settings">Settings for how to execute the query, from the user</param>
        /// <param name="outputFactory">Factory for creating output files</param>
        public Query(string queryText, ConnectionInfo connection, QueryExecutionSettings settings, IFileStreamFactory outputFactory)
        {
            // Sanity check for input
            Validate.IsNotNullOrEmptyString(nameof(queryText), queryText);
            Validate.IsNotNull(nameof(connection), connection);
            Validate.IsNotNull(nameof(settings), settings);
            Validate.IsNotNull(nameof(outputFactory), outputFactory);

            // Initialize the internal state
            QueryText = queryText;
            editorConnection = connection;
            cancellationSource = new CancellationTokenSource();

            // Process the query into batches
            ParseResult parseResult = Parser.Parse(queryText, new ParseOptions
            {
                BatchSeparator = settings.BatchSeparator
            });
            // NOTE: We only want to process batches that have statements (ie, ignore comments and empty lines)
            var batchSelection = parseResult.Script.Batches
                .Where(batch => batch.Statements.Count > 0)
                .Select((batch, index) =>
                    new Batch(batch.Sql,
                        new SelectionData(
                            batch.StartLocation.LineNumber - 1,
                            batch.StartLocation.ColumnNumber - 1,
                            batch.EndLocation.LineNumber - 1,
                            batch.EndLocation.ColumnNumber - 1),
                        index, outputFactory));

            Batches = batchSelection.ToArray();


            // Create our batch lists
            BeforeBatches = new List<Batch>();
            AfterBatches = new List<Batch>();

            // place holders until implemented
            ReliableConnectionHelper.ServerInfo serverInfo = ReliableConnectionHelper.GetServerVersion(connection.SqlConnection);
            bool isSqlDw = (serverInfo.EngineEditionId == (int)DatabaseEngineEdition.SqlDataWarehouse);

            // should i also cover the Sql Server CE case?
            
            if (!isSqlDw)
            {
                // Client statistics and showplan are not shown for multi-server connections.  If we do decide to show
                // showplan or statistics for multi-server connections, each child server connection will need
                // its own setting because the child servers do not have to be the same version.
                // TODO: support multiServerConnection
                //showplan specified via UI corresponds to execOptions.WithShowPlan option and takes precedence
                //over all other showplan related settings that might have been specified via Connection Settings
                //UI
                if (settings.ExecutionPlanOptions.IncludeEstimatedExecutionPlan) 
                {
                    if (serverInfo.ServerMajorVersion >= 9)
                    {
                        // Enable set showplan xml
                        addBatch(string.Format(s_SetShowPlanXml, s_On), BeforeBatches, outputFactory);
                        addBatch(string.Format(s_SetShowPlanXml, s_Off), AfterBatches, outputFactory);
                    }
                    else
                    {
                        // Enable set showplan all
                        addBatch(string.Format(s_SetShowPlanAll, s_On), BeforeBatches, outputFactory);
                        addBatch(string.Format(s_SetShowPlanAll, s_Off), AfterBatches, outputFactory);
                    }
                }
                // check for the actual exectuion plan (statistics xml)
                else if (settings.ExecutionPlanOptions.IncludeActualExecutionPlan)
                {
                    if (serverInfo.ServerMajorVersion >= 9)
                    {
                        // enable set statistics xml
                        addBatch(string.Format(s_SetStatisticsXml, s_On), BeforeBatches, outputFactory);
                        addBatch(string.Format(s_SetStatisticsXml, s_Off), AfterBatches, outputFactory);
                    
                        // live showplan enabling goes here in the future
                    } 
                    else
                    {
                        // enable set statistics profile
                        addBatch(string.Format(s_SetStatisticsProfile, s_On), BeforeBatches, outputFactory);
                        addBatch(string.Format(s_SetStatisticsProfile, s_Off), AfterBatches, outputFactory);
                    }
                }
            }
        }



        #region Events

        /// <summary>
        /// Event to be called when a batch is completed.
        /// </summary>
        public event Batch.BatchAsyncEventHandler BatchCompleted;

        /// <summary>
        /// Event to be called when a batch starts execution.
        /// </summary>
        public event Batch.BatchAsyncEventHandler BatchStarted;

        /// <summary>
        /// Delegate type for callback when a query connection fails
        /// </summary>
        /// <param name="message">Error message for the failing query</param>
        public delegate Task QueryAsyncErrorEventHandler(string message);

        /// <summary>
        /// Callback for when the query has completed successfully
        /// </summary>
        public event QueryAsyncEventHandler QueryCompleted;

        /// <summary>
        /// Callback for when the query has failed
        /// </summary>
        public event QueryAsyncEventHandler QueryFailed;

        /// <summary>
        /// Callback for when the query connection has failed
        /// </summary>
        public event QueryAsyncErrorEventHandler QueryConnectionException;

        /// <summary>
        /// Event to be called when a resultset has completed.
        /// </summary>
        public event ResultSet.ResultSetAsyncEventHandler ResultSetCompleted;

        #endregion

        #region Properties

        /// <summary>
        /// Delegate type for callback when a query completes or fails
        /// </summary>
        /// <param name="q">The query that completed</param>
        public delegate Task QueryAsyncEventHandler(Query q);

        /// <summary>
        /// The batches which should run before the user batches 
        /// </summary>
        internal List<Batch> BeforeBatches { get; set; }

        /// <summary>
        /// The batches underneath this query
        /// </summary>
        internal Batch[] Batches { get; set; }

        /// <summary>
        /// The batches which should run after the user batches 
        /// </summary>
        internal List<Batch> AfterBatches { get; set; }

        /// <summary>
        /// The summaries of the batches underneath this query
        /// </summary>
        public BatchSummary[] BatchSummaries
        {
            get
            {
                if (!HasExecuted)
                {
                    throw new InvalidOperationException("Query has not been executed.");
                }
                return Batches.Select(b => b.Summary).ToArray();
            }
        }

        /// <summary>
        /// Storage for the async task for execution. Set as internal in order to await completion
        /// in unit tests.
        /// </summary>
        internal Task ExecutionTask { get; private set; }

        /// <summary>
        /// Whether or not the query has completed executed, regardless of success or failure
        /// </summary>
        /// <remarks>
        /// Don't touch the setter unless you're doing unit tests!
        /// </remarks>
        public bool HasExecuted
        {
            get { return Batches.Length == 0 ? hasExecuteBeenCalled : Batches.All(b => b.HasExecuted); }
            internal set
            {
                hasExecuteBeenCalled = value;
                foreach (var batch in Batches)
                {
                    batch.HasExecuted = value;
                }
            }
        }

        /// <summary>
        /// The text of the query to execute
        /// </summary>
        public string QueryText { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Cancels the query by issuing the cancellation token
        /// </summary>
        public void Cancel()
        {
            // Make sure that the query hasn't completed execution
            if (HasExecuted)
            {
                throw new InvalidOperationException(SR.QueryServiceCancelAlreadyCompleted);
            }

            // Issue the cancellation token for the query
            cancellationSource.Cancel();
        }

        /// <summary>
        /// Launches the asynchronous process for executing the query
        /// </summary>
        public void Execute()
        {
            ExecutionTask = Task.Run(ExecuteInternal);
        }

        /// <summary>
        /// Retrieves a subset of the result sets
        /// </summary>
        /// <param name="batchIndex">The index for selecting the batch item</param>
        /// <param name="resultSetIndex">The index for selecting the result set</param>
        /// <param name="startRow">The starting row of the results</param>
        /// <param name="rowCount">How many rows to retrieve</param>
        /// <returns>A subset of results</returns>
        public Task<ResultSetSubset> GetSubset(int batchIndex, int resultSetIndex, int startRow, int rowCount)
        {
            // Sanity check to make sure that the batch is within bounds
            if (batchIndex < 0 || batchIndex >= Batches.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(batchIndex), SR.QueryServiceSubsetBatchOutOfRange);
            }

            return Batches[batchIndex].GetSubset(resultSetIndex, startRow, rowCount);
        }

        /// <summary>
        /// Retrieves a subset of the result sets
        /// </summary>
        /// <param name="batchIndex">The index for selecting the batch item</param>
        /// <param name="resultSetIndex">The index for selecting the result set</param>
        /// <returns>The Execution Plan, if the result set has one</returns>
        public Task<ExecutionPlan> GetExecutionPlan(int batchIndex, int resultSetIndex)
        {
            // Sanity check to make sure that the batch is within bounds
            if (batchIndex < 0 || batchIndex >= Batches.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(batchIndex), SR.QueryServiceSubsetBatchOutOfRange);
            }

            return Batches[batchIndex].GetExecutionPlan(resultSetIndex);
        }

        /// <summary>
        /// Saves the requested results to a file format of the user's choice
        /// </summary>
        /// <param name="saveParams">Parameters for the save as request</param>
        /// <param name="fileFactory">
        /// Factory for creating the reader/writer pair for the requested output format
        /// </param>
        /// <param name="successHandler">Delegate to call when the request completes successfully</param>
        /// <param name="failureHandler">Delegate to call if the request fails</param>
        public void SaveAs(SaveResultsRequestParams saveParams, IFileStreamFactory fileFactory, 
            ResultSet.SaveAsAsyncEventHandler successHandler, ResultSet.SaveAsFailureAsyncEventHandler failureHandler)
        {
            // Sanity check to make sure that the batch is within bounds
            if (saveParams.BatchIndex < 0 || saveParams.BatchIndex >= Batches.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(saveParams.BatchIndex), SR.QueryServiceSubsetBatchOutOfRange);
            }

            Batches[saveParams.BatchIndex].SaveAs(saveParams, fileFactory, successHandler, failureHandler);
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Executes this query asynchronously and collects all result sets
        /// </summary>
        private async Task ExecuteInternal()
        {
            // Mark that we've internally executed
            hasExecuteBeenCalled = true;

            // Don't actually execute if there aren't any batches to execute
            if (Batches.Length == 0)
            {
                return;
            }

            // Open up a connection for querying the database
            string connectionString = ConnectionService.BuildConnectionString(editorConnection.ConnectionDetails);
            // TODO: Don't create a new connection every time, see TFS #834978
            using (DbConnection conn = editorConnection.Factory.CreateSqlConnection(connectionString))
            {
                try
                {
                    await conn.OpenAsync();
                }
                catch (Exception exception)
                {
                    this.HasExecuted = true;
                    if (QueryConnectionException != null)
                    {
                        await QueryConnectionException(exception.Message);
                    }
                    return;
                }

                ReliableSqlConnection sqlConn = conn as ReliableSqlConnection;
                if (sqlConn != null)
                {
                    // Subscribe to database informational messages
                    sqlConn.GetUnderlyingConnection().InfoMessage += OnInfoMessage;
                }

                try
                {

                    // Execute beforeBatches synchronously, before the user defined batches 
                    foreach (Batch b in BeforeBatches)
                    {
                        await b.Execute(conn, cancellationSource.Token);
                    }

                    // We need these to execute synchronously, otherwise the user will be very unhappy
                    foreach (Batch b in Batches)
                    {
                        // Attach extension callbacks to the these batches because the user explicity ran them
                        b.BatchStart += BatchStarted;
                        b.BatchCompletion += BatchCompleted;
                        b.ResultSetCompletion += ResultSetCompleted;
                        await b.Execute(conn, cancellationSource.Token);
                    }

                    // Execute afterBatches synchronously, after the user defined batches
                    foreach (Batch b in AfterBatches)
                    {
                        await b.Execute(conn, cancellationSource.Token);
                    }

                    // Call the query execution callback
                    if (QueryCompleted != null)
                    {
                        await QueryCompleted(this);
                    }
                }
                catch (Exception)
                {
                    // Call the query failure callback
                    if (QueryFailed != null)
                    {
                        await QueryFailed(this);
                    }
                }
                finally
                {
                    if (sqlConn != null)
                    {
                        // Subscribe to database informational messages
                        sqlConn.GetUnderlyingConnection().InfoMessage -= OnInfoMessage;
                    }
                }

                // TODO: Close connection after eliminating using statement for above TODO
            }
        }

        /// <summary>
        /// Handler for database messages during query execution
        /// </summary>
        private void OnInfoMessage(object sender, SqlInfoMessageEventArgs args)
        {
            SqlConnection conn = sender as SqlConnection;
            if (conn == null)
            {
                throw new InvalidOperationException(SR.QueryServiceMessageSenderNotSql);
            }

            foreach (SqlError error in args.Errors)
            {
                // Did the database context change (error code 5701)?
                if (error.Number == DatabaseContextChangeErrorNumber)
                {
                    ConnectionService.Instance.ChangeConnectionDatabaseContext(editorConnection.OwnerUri, conn.Database);
                }
            }
        }

        /// <summary>
        /// Function to add a new batch to a Batch set
        /// </summary>
        private void addBatch(string query, List<Batch> batchSet, IFileStreamFactory outputFactory)
        {
            batchSet.Add(new Batch(query, new SelectionData(0,0,0,0), 0, outputFactory));
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                cancellationSource.Dispose();
                foreach (Batch b in Batches)
                {
                    b.Dispose();
                }
            }

            disposed = true;
        }

        #endregion
    }
}
