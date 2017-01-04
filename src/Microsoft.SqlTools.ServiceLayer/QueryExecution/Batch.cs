﻿// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution
{
    /// <summary>
    /// This class represents a batch within a query
    /// </summary>
    public class Batch : IDisposable
    {
        #region Member Variables

        /// <summary>
        /// For IDisposable implementation, whether or not this has been disposed
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Local time when the execution and retrieval of files is finished
        /// </summary>
        private DateTime executionEndTime;

        /// <summary>
        /// Local time when the execution starts, specifically when the object is created
        /// </summary>
        private DateTime executionStartTime;

        /// <summary>
        /// Factory for creating readers/writers for the output of the batch
        /// </summary>
        private readonly IFileStreamFactory outputFileFactory;

        /// <summary>
        /// Internal representation of the messages so we can modify internally
        /// </summary>
        internal readonly List<ResultMessage> resultMessages;

        /// <summary>
        /// Internal representation of the result sets so we can modify internally
        /// </summary>
        private readonly List<ResultSet> resultSets;

        public enum BatchSpecialAction
        {
            None = 0x0, 
            ExpectActualExecutionPlan = 0x1, 
            ExpectEstimatedExecutionPlan = 0x2,
            ExpectActualYukonXmlShowPlan = 0x4,
            ExpectEstimatedYukonXmlShowPlan	= 0x8,
            ExecuteWithDebugging = 0x10,
            ExecuteLivePlan = 0x20,
            HideYukonXMLShowPlan = 0x40,
            ExpectYukonXmlShowPlan  = ExpectActualYukonXmlShowPlan | ExpectEstimatedYukonXmlShowPlan,
            ShowPlanMask = ExpectYukonXmlShowPlan | ExpectActualExecutionPlan | ExpectEstimatedExecutionPlan
        }

        #endregion

        internal Batch(string batchText, SelectionData selection, int ordinalId, IFileStreamFactory outputFileFactory)
        {
            // Sanity check for input
            Validate.IsNotNullOrEmptyString(nameof(batchText), batchText);
            Validate.IsNotNull(nameof(outputFileFactory), outputFileFactory);
            Validate.IsGreaterThan(nameof(ordinalId), ordinalId, 0);

            // Initialize the internal state
            BatchText = batchText;
            Selection = selection;
            executionStartTime = DateTime.Now;
            HasExecuted = false;
            Id = ordinalId;
            resultSets = new List<ResultSet>();
            resultMessages = new List<ResultMessage>();
            this.outputFileFactory = outputFileFactory;
        }

        #region Events

        /// <summary>
        /// Asynchronous handler for when batches are completed
        /// </summary>
        /// <param name="batch">The batch that completed</param>
        public delegate Task BatchAsyncEventHandler(Batch batch);

        /// <summary>
        /// Event that will be called when the batch has completed execution
        /// </summary>
        public event BatchAsyncEventHandler BatchCompletion;

        /// <summary>
        /// Event to call when the batch has started execution
        /// </summary>
        public event BatchAsyncEventHandler BatchStart;

        /// <summary>
        /// Event that will be called when the resultset has completed execution. It will not be
        /// called from the Batch but from the ResultSet instance
        /// </summary>
        public event ResultSet.ResultSetAsyncEventHandler ResultSetCompletion;

        #endregion

        #region Properties

        /// <summary>
        /// The text of batch that will be executed
        /// </summary>
        public string BatchText { get; set; }

        /// <summary>
        /// Localized timestamp for when the execution completed.
        /// Stored in UTC ISO 8601 format; should be localized before displaying to any user
        /// </summary>
        public string ExecutionEndTimeStamp { get { return executionEndTime.ToString("o"); } }

        /// <summary>
        /// Localized timestamp for how long it took for the execution to complete
        /// </summary>
        public string ExecutionElapsedTime
        {
            get
            {
                TimeSpan elapsedTime = executionEndTime - executionStartTime;
                return elapsedTime.ToString();
            }
        }

        /// <summary>
        /// Localized timestamp for when the execution began.
        /// Stored in UTC ISO 8601 format; should be localized before displaying to any user
        /// </summary>
        public string ExecutionStartTimeStamp { get { return executionStartTime.ToString("o"); } }

        /// <summary>
        /// Whether or not this batch has an error
        /// </summary>
        public bool HasError { get; set; }

        /// <summary>
        /// Whether or not this batch has been executed, regardless of success or failure 
        /// </summary>
        public bool HasExecuted { get; set; }

        /// <summary>
        /// Ordinal of the batch in the query
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Messages that have come back from the server
        /// </summary>
        public IEnumerable<ResultMessage> ResultMessages
        {
            get { return resultMessages; }
        }

        /// <summary>
        /// The result sets of the batch execution
        /// </summary>
        public IList<ResultSet> ResultSets
        {
            get { return resultSets; }
        }

        /// <summary>
        /// Property for generating a set result set summaries from the result sets
        /// </summary>
        public ResultSetSummary[] ResultSummaries
        {
            get
            {
                lock (resultSets)
                {
                    return resultSets.Select((set) => set.Summary).ToArray();
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="BatchSummary"/> based on the batch instance
        /// </summary>
        public BatchSummary Summary
        {
            get
            {
                // Batch summary with information available at start
                BatchSummary summary = new BatchSummary
                {
                    HasError = HasError,
                    Id = Id,
                    Selection = Selection,
                    ExecutionStart = ExecutionStartTimeStamp
                };

                // Add on extra details if we finished executing it
                if (HasExecuted)
                {
                    summary.ResultSetSummaries = ResultSummaries;
                    summary.Messages = ResultMessages.ToArray();
                    summary.ExecutionEnd = ExecutionEndTimeStamp;
                    summary.ExecutionElapsed = ExecutionElapsedTime;
                    summary.BatchSpecialAction = processResultSetsSpecialActions();
                }

                return summary;
            }
        }

        /// <summary>
        /// The range from the file that is this batch
        /// </summary>
        internal SelectionData Selection { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Executes this batch and captures any server messages that are returned.
        /// </summary>
        /// <param name="conn">The connection to use to execute the batch</param>
        /// <param name="cancellationToken">Token for cancelling the execution</param>
        public async Task Execute(DbConnection conn, CancellationToken cancellationToken)
        {
            // Sanity check to make sure we haven't already run this batch
            if (HasExecuted)
            {
                throw new InvalidOperationException("Batch has already executed.");
            }

            // Notify that we've started execution
            if (BatchStart != null)
            {
                await BatchStart(this);
            }

            try
            {
                // Register the message listener to *this instance* of the batch
                // Note: This is being done to associate messages with batches
                ReliableSqlConnection sqlConn = conn as ReliableSqlConnection;
                DbCommand command;
                if (sqlConn != null)
                {
                    // Register the message listener to *this instance* of the batch
                    // Note: This is being done to associate messages with batches
                    sqlConn.GetUnderlyingConnection().InfoMessage += StoreDbMessage;
                    command = sqlConn.GetUnderlyingConnection().CreateCommand();

                    // Add a handler for when the command completes
                    SqlCommand sqlCommand = (SqlCommand)command;
                    sqlCommand.StatementCompleted += StatementCompletedHandler;
                }
                else
                {
                    command = conn.CreateCommand();
                }

                // Make sure we aren't using a ReliableCommad since we do not want automatic retry
                Debug.Assert(!(command is ReliableSqlConnection.ReliableSqlCommand),
                    "ReliableSqlCommand command should not be used to execute queries");

                // Create a command that we'll use for executing the query
                using (command)
                {
                    command.CommandText = BatchText;
                    command.CommandType = CommandType.Text;
                    command.CommandTimeout = 0;
                    executionStartTime = DateTime.Now;

                    // Execute the command to get back a reader
                    using (DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        int resultSetOrdinal = 0;
                        do
                        {
                            // Skip this result set if there aren't any rows (ie, UPDATE/DELETE/etc queries)
                            if (!reader.HasRows && reader.FieldCount == 0)
                            {
                                continue;
                            }

                            // This resultset has results (ie, SELECT/etc queries)
                            ResultSet resultSet = new ResultSet(reader, resultSetOrdinal, Id, outputFileFactory);
                            resultSet.ResultCompletion += ResultSetCompletion;

                            // Add the result set to the results of the query
                            lock (resultSets)
                            {
                                resultSets.Add(resultSet);
                                resultSetOrdinal++;
                            }

                            // Read until we hit the end of the result set
                            await resultSet.ReadResultToEnd(cancellationToken).ConfigureAwait(false);

                        } while (await reader.NextResultAsync(cancellationToken));

                        // If there were no messages, for whatever reason (NO COUNT set, messages 
                        // were emitted, records returned), output a "successful" message
                        if (resultMessages.Count == 0)
                        {
                            resultMessages.Add(new ResultMessage(SR.QueryServiceCompletedSuccessfully));
                        }
                    }
                }
            }
            catch (DbException dbe)
            {
                HasError = true;
                UnwrapDbException(dbe);
            }
            catch (TaskCanceledException)
            {
                resultMessages.Add(new ResultMessage(SR.QueryServiceQueryCancelled));
                throw;
            }
            catch (Exception e)
            {
                HasError = true;
                resultMessages.Add(new ResultMessage(SR.QueryServiceQueryFailed(e.Message)));
                throw;
            }
            finally
            {
                // Remove the message event handler from the connection
                ReliableSqlConnection sqlConn = conn as ReliableSqlConnection;
                if (sqlConn != null)
                {
                    sqlConn.GetUnderlyingConnection().InfoMessage -= StoreDbMessage;
                }

                // Mark that we have executed
                HasExecuted = true;
                executionEndTime = DateTime.Now;

                // Fire an event to signify that the batch has completed
                if (BatchCompletion != null)
                {
                    await BatchCompletion(this);
                }
            }
        }

        /// <summary>
        /// Generates a subset of the rows from a result set of the batch
        /// </summary>
        /// <param name="resultSetIndex">The index for selecting the result set</param>
        /// <param name="startRow">The starting row of the results</param>
        /// <param name="rowCount">How many rows to retrieve</param>
        /// <returns>A subset of results</returns>
        public Task<ResultSetSubset> GetSubset(int resultSetIndex, int startRow, int rowCount)
        {
            ResultSet targetResultSet;
            lock (resultSets)
            {
                // Sanity check to make sure we have valid numbers
                if (resultSetIndex < 0 || resultSetIndex >= resultSets.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(resultSetIndex),
                        SR.QueryServiceSubsetResultSetOutOfRange);
                }

                targetResultSet = resultSets[resultSetIndex];
            }

            // Retrieve the result set
            return targetResultSet.GetSubset(startRow, rowCount);
        }

        /// <summary>
        /// Saves a result to a file format selected by the user
        /// </summary>
        /// <param name="saveParams">Parameters for the save as request</param>
        /// <param name="fileFactory">
        /// Factory for creating the reader/writer pair for outputing to the selected format
        /// </param>
        /// <param name="successHandler">Delegate to call when request successfully completes</param>
        /// <param name="failureHandler">Delegate to call if the request fails</param>
        public void SaveAs(SaveResultsRequestParams saveParams, IFileStreamFactory fileFactory,
            ResultSet.SaveAsAsyncEventHandler successHandler, ResultSet.SaveAsFailureAsyncEventHandler failureHandler)
        {
            // Get the result set to save
            ResultSet resultSet;
            lock (resultSets)
            {
                // Sanity check to make sure we have a valid result set
                if (saveParams.ResultSetIndex < 0 || saveParams.ResultSetIndex >= resultSets.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(saveParams.BatchIndex), SR.QueryServiceSubsetResultSetOutOfRange);
                }


                resultSet = resultSets[saveParams.ResultSetIndex];
            }
            resultSet.SaveAs(saveParams, fileFactory, successHandler, failureHandler);
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Handler for when the StatementCompleted event is fired for this batch's command. This
        /// will be executed ONLY when there is a rowcount to report. If this event is not fired
        /// either NOCOUNT has been set or the command doesn't affect records.
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="args">Arguments for the event</param>
        internal void StatementCompletedHandler(object sender, StatementCompletedEventArgs args)
        {
            // Add a message for the number of rows the query returned
            string message;
            if (args.RecordCount == 1)
            {
                message = SR.QueryServiceAffectedOneRow;
            }
            else
            {
                message = SR.QueryServiceAffectedRows(args.RecordCount);
            }

            resultMessages.Add(new ResultMessage(message));
        }

        /// <summary>
        /// Delegate handler for storing messages that are returned from the server
        /// NOTE: Only messages that are below a certain severity will be returned via this
        /// mechanism. Anything above that level will trigger an exception.
        /// </summary>
        /// <param name="sender">Object that fired the event</param>
        /// <param name="args">Arguments from the event</param>
        private void StoreDbMessage(object sender, SqlInfoMessageEventArgs args)
        {
            resultMessages.Add(new ResultMessage(args.Message));
        }

        /// <summary>
        /// Attempts to convert a <see cref="DbException"/> to a <see cref="SqlException"/> that
        /// contains much more info about Sql Server errors. The exception is then unwrapped and
        /// messages are formatted and stored in <see cref="ResultMessages"/>. If the exception
        /// cannot be converted to SqlException, the message is written to the messages list.
        /// </summary>
        /// <param name="dbe">The exception to unwrap</param>
        internal void UnwrapDbException(DbException dbe)
        {
            SqlException se = dbe as SqlException;
            if (se != null)
            {
                var errors = se.Errors.Cast<SqlError>().ToList();
                // Detect user cancellation errors
                if (errors.Any(error => error.Class == 11 && error.Number == 0))
                {
                    // User cancellation error, add the single message
                    HasError = false;
                    resultMessages.Add(new ResultMessage(SR.QueryServiceQueryCancelled));
                }
                else
                {
                    // Not a user cancellation error, add all 
                    foreach (var error in errors)
                    {
                        int lineNumber = error.LineNumber + Selection.StartLine;
                        string message = string.Format("Msg {0}, Level {1}, State {2}, Line {3}{4}{5}",
                            error.Number, error.Class, error.State, lineNumber,
                            Environment.NewLine, error.Message);
                        resultMessages.Add(new ResultMessage(message));
                    }
                }
            }
            else
            {
                resultMessages.Add(new ResultMessage(dbe.Message));
            }
        }


        private BatchSpecialAction processResultSetsSpecialActions()
        {
            BatchSpecialAction batchSpecialAction = BatchSpecialAction.None;
            foreach (ResultSet resultSet in resultSets) 
            {
                batchSpecialAction |= resultSet.Summary.SpecialAction;
            }

            return batchSpecialAction;
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
                lock (resultSets)
                {
                    foreach (ResultSet r in resultSets)
                    {
                        r.Dispose();
                    }
                }
            }

            disposed = true;
        }

        #endregion
    }
}
