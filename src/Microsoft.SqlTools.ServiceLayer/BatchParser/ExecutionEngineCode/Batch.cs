//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    /// <summary>
    /// Single batch of SQL command
    /// </summary>
    internal class Batch
    {
        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public Batch()
        {
            // nothing
        }

        /// <summary>
        /// Creates and initializes a batch object
        /// </summary>
        /// <param name="isResultExpected">Whether it is one of "set [something] on/off" type of command,
        /// that doesn't return any results from the server
        /// </param>
        /// <param name="sqlText">Text of the batch</param>
        /// <param name="execTimeout">Timeout for the batch execution. 0 means no limit </param>
        public Batch(String sqlText, bool isResultExpected, int execTimeout)
        {
            _isResultExpected = isResultExpected;
            _sqlText = sqlText;
            _execTimeout = execTimeout;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Is the Batch's text valid?
        /// </summary>
        public bool HasValidText
        {
            get
            {
                return !String.IsNullOrEmpty(_sqlText);
            }
        }

        /// <summary>
        /// SQL text that to be executed in the Batch
        /// </summary>
        public String Text
        {
            get
            {
                return _sqlText;
            }

            set
            {
                _sqlText = value;
            }
        }


        /// <summary>
        /// Determines whether batch execution returns any results
        /// </summary>
        public bool IsResultsExpected
        {
            get
            {
                return _isResultExpected;
            }

            set
            {
                _isResultExpected = value;
            }
        }

        /// <summary>
        /// Determines the execution timeout for the batch
        /// </summary>
        public int ExecutionTimeout
        {
            get
            {
                return _execTimeout;
            }

            set
            {
                _execTimeout = value;
            }
        }

        /// <summary>
        /// Determines the textspan to wich the batch belongs to
        /// </summary>
        public TextSpan TextSpan
        {
            get
            {
                return _textSpan;
            }
            set
            {
                _textSpan = value;
            }
        }

        /// <summary>
        /// Determines the batch index in the collection of batches being executed
        /// </summary>
        public int BatchIndex
        {
            get
            {
                return _index;
            }

            set
            {
                _index = value;
            }
        }


        /// <summary>
        /// Returns how many rows were affected. It should be the value that can be shown
        /// in the UI. 
        /// </summary>
        /// <remarks>
        /// It can be used only after the execution of the batch is finished
        /// </remarks>
        public long RowsAffected
        {
            get
            {
                return _totalAffectedRows;
            }
        }

        /// <summary>
        /// Determines if the error.Source should be used when messages are written
        /// </summary>
        public bool IsSuppressProviderMessageHeaders
        {
            get
            {
                return _isSuppressProviderMessageHeaders;
            }
            set
            {
                _isSuppressProviderMessageHeaders = value;
            }
        }

        /// <summary>
        /// Gets or sets the id of the script we are tracking
        /// </summary>
        public int ScriptTrackingId
        {
            get
            {
                return _scriptTrackingId;
            }
            set
            {
                _scriptTrackingId = value;
            }
        }

        // TODO Reenable if any consumer ever needs specialized script tracking. Only Dacfx needs at present and
        // has its own copy of this code.
        /// <summary>
        /// Gets or sets a value indicating whether to track the current batch in the tracking table
        /// </summary>
        //public bool IsScriptExecutionTracked
        //{
        //    get
        //    {
        //        return _isScriptExecutionTracked;
        //    }
        //    set
        //    {
        //        _isScriptExecutionTracked = value;
        //    }
        //}

        #endregion

        #region Public events

        /// <summary>
        /// fired when there is an error message from the server
        /// </summary>
        public event EventHandler<BatchErrorEventArgs> BatchError = null;

        /// <summary>
        /// fired when there is a message from the server
        /// </summary>
        public event EventHandler<BatchMessageEventArgs> BatchMessage = null;

        /// <summary>
        /// fired when there is a new result set available. It is guarnteed
        /// to be fired from the same thread that called Execute method
        /// </summary>
        public event EventHandler<BatchResultSetEventArgs> BatchResultSetProcessing = null;

        /// <summary>
        /// fired when the batch recieved cancel request BEFORE it 
        /// initiates cancel operation. Note that it is fired from a
        /// different thread then the one used to kick off execution
        /// </summary>
        public event EventHandler<EventArgs> BatchCancelling = null;

        /// <summary>
        /// fired when we've done absolutely all actions for the current result set
        /// </summary>
        public event EventHandler<EventArgs> BatchResultSetFinished = null;
        #endregion

        #region Public methods

        /// <summary>
        /// Resets the object to its initial state
        /// </summary>
        public void Reset()
        {
            lock (this)
            {
                _state = BatchState.Initial;
                _command = null;
                _textSpan = new TextSpan();
                _totalAffectedRows = 0;
                _hasErrors = false;
                _expectedShowPlan = ShowPlanType.None;
                _isSuppressProviderMessageHeaders = false;
                _scriptTrackingId = 0;
                _isScriptExecutionTracked = false;
            }
        }

        /// <summary>
        /// Executes the batch 
        /// </summary>
        /// <param name="connection">Connection to use</param>
        /// <param name="expectedShowPlan">ShowPlan type to be used</param>
        /// <returns>result of execution</returns>
        /// <remarks>
        /// It does not return until execution is finished
        /// We may have received a Cancel request by the time this function is called
        /// </remarks>
        public ScriptExecutionResult Execute(SqlConnection connection, ShowPlanType expectedShowPlan)
        {
            // FUTURE CLEANUP: Remove in favor of general signature (IDbConnection) - #920978
            return Execute((IDbConnection)connection, expectedShowPlan);
        }

        /// <summary>
        /// Executes the batch 
        /// </summary>
        /// <param name="connection">Connection to use</param>
        /// <param name="expectedShowPlan">ShowPlan type to be used</param>
        /// <returns>result of execution</returns>
        /// <remarks>
        /// It does not return until execution is finished
        /// We may have received a Cancel request by the time this function is called
        /// </remarks>
        public ScriptExecutionResult Execute(IDbConnection connection, ShowPlanType expectedShowPlan)
        {

            Validate.IsNotNull(nameof(connection), connection);

            //makes sure that the batch is not in use
            lock (this)
            {
                Debug.Assert(_command == null, "SQLCommand is NOT null");
                if (_command != null)
                {
                    _command = null;
                }
            }

            _expectedShowPlan = expectedShowPlan;

            return DoBatchExecutionImpl(connection, _sqlText);
        }

        /// <summary>
        /// Cancels the batch
        /// </summary>
        /// <remarks>
        /// When batch is actually cancelled, Execute() will return with the appropiate status 
        /// </remarks>
        public void Cancel()
        {
            lock (this)
            {
                if (_state != BatchState.Cancelling)
                {
                    _state = BatchState.Cancelling;

                    RaiseCancelling();

                    if (_command != null)
                    {
                        try
                        {
                            _command.Cancel();

                            Debug.WriteLine("Batch.Cancel: _command.Cancel completed");
                        }
                        catch (SqlException)
                        {
                            // eat it
                        }
                        catch (RetryLimitExceededException)
                        {
                            // eat it
                        }
                    }
                }
            }
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Fires an error message event
        /// </summary>
        /// <param name="ex">Exception cought</param>
        /// <remarks>
        /// Non-SQL exception
        /// </remarks>
        protected void HandleExceptionMessage(Exception ex)
        {
            BatchErrorEventArgs args = new BatchErrorEventArgs(String.Format(CultureInfo.CurrentCulture, SR.EE_BatchError_Exception, ex.Message), ex);
            RaiseBatchError(args);
        }

        /// <summary>
        /// Fires a message event
        /// </summary>
        /// <param name="errors">SqlClient errors collection</param>
        /// <remarks>
        /// Sql specific messages.
        /// </remarks>
        protected void HandleSqlMessages(SqlErrorCollection errors)
        {
            foreach (SqlError error in errors)
            {
                if (error.Number == ChangeDatabase)
                {
                    continue;
                }

                String detailedMessage = FormatSqlErrorMessage(error);

                if (error.Class > 10)
                {
                    // expose this event as error
                    Debug.Assert(detailedMessage.Length != 0);
                    RaiseBatchError(detailedMessage, error, _textSpan);

                    //at least one error message has been used
                    _hasErrors = true;
                }
                else
                {
                    RaiseBatchMessage(detailedMessage, error.Message, error);
                }
            }
        }

        /// <summary>
        /// method that will be passed as delegate to SqlConnection.InfoMessage
        /// </summary>
        protected void OnSqlInfoMessageCallback(object sender, SqlInfoMessageEventArgs e)
        {
            HandleSqlMessages(e.Errors);
        }

        /// <summary>
        /// Delegete for SqlCommand.RecordsAffected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>
        /// This is exposed as a regular message
        /// </remarks>
        protected void OnStatementExecutionFinished(object sender, StatementCompletedEventArgs e)
        {
            String message = String.Format(CultureInfo.CurrentCulture, SR.EE_BatchExecutionInfo_RowsAffected,
                e.RecordCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
            RaiseBatchMessage(message, message, null);
        }

        /// <summary>
        /// Called on a new ResultSet on the data reader
        /// </summary>
        /// <param name="dataReader">True if result set consumed, false on a Cancel request</param>
        /// <returns></returns>
        /// <remarks>
        /// The GridStorageResultSet created is owned by the batch consumer. It's only created here.
        /// Additionally, when BatchResultSet event handler is called, it won't return until
        /// all data is prcessed or the data being processed is terminated (i.e. cancel or error)
        /// </remarks>
        protected ScriptExecutionResult ProcessResultSet(IDataReader dataReader)
        {
            if (dataReader == null)
            {
                throw new ArgumentNullException();
            }

            Debug.WriteLine("ProcessResultSet: result set has been created");

            //initialize result variable that will be set by batch consumer
            ScriptExecutionResult scriptExecutionResult = ScriptExecutionResult.Success;

            RaiseBatchResultSetProcessing(dataReader, _expectedShowPlan);

            if (_state != BatchState.Cancelling)
            {
                return scriptExecutionResult;
            }
            else
            {
                return ScriptExecutionResult.Cancel;
            }
        }

        // FUTURE CLEANUP: Remove in favor of general signature (IDbConnection) - #920978
        protected ScriptExecutionResult DoBatchExecution(SqlConnection connection, String script)
        {
            return DoBatchExecutionImpl(connection, script);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities"), SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        [SuppressMessage("Microsoft.Usage", "CA2219:DoNotRaiseExceptionsInExceptionClauses")]
        private ScriptExecutionResult DoBatchExecutionImpl(IDbConnection connection, String script)
        {
            Validate.IsNotNull(nameof(connection), connection);

            lock (this)
            {
                if (_state == BatchState.Cancelling)
                {
                    _state = BatchState.Initial;
                    return ScriptExecutionResult.Cancel;
                }
            }

            ScriptExecutionResult result = ScriptExecutionResult.Success;

            // SqlClient event handlers setup
            SqlInfoMessageEventHandler messageHandler = new SqlInfoMessageEventHandler(OnSqlInfoMessageCallback);
            StatementCompletedEventHandler statementCompletedHandler = null;

            DbConnectionWrapper connectionWrapper = new DbConnectionWrapper(connection);
            connectionWrapper.InfoMessage += messageHandler;

            IDbCommand command = connection.CreateCommand();
            command.CommandText = script;
            command.CommandTimeout = _execTimeout;

            DbCommandWrapper commandWrapper = null;
            if (_isScriptExecutionTracked && DbCommandWrapper.IsSupportedCommand(command))
            {
                statementCompletedHandler = new StatementCompletedEventHandler(OnStatementExecutionFinished);
                commandWrapper = new DbCommandWrapper(command);
                commandWrapper.StatementCompleted += statementCompletedHandler;
            }

            lock (this)
            {
                _state = BatchState.Executing;
                _command = command;
                command = null;
            }

            try
            {
                result = this.ExecuteCommand();
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (SqlException sqlEx)
            {
                result = HandleSqlException(sqlEx);
            }
            catch (Exception ex)
            {
                result = ScriptExecutionResult.Failure;
                HandleExceptionMessage(ex);
            }
            finally
            {
               
                if (messageHandler == null)
                {
                    Logger.Write(LogLevel.Error, "Expected handler to be declared");
                }

                if (null != connectionWrapper)
                {
                    connectionWrapper.InfoMessage -= messageHandler;
                }

                if (commandWrapper != null)
                {

                    if (statementCompletedHandler == null)
                    {
                        Logger.Write(LogLevel.Error, "Expect handler to be declared if we have a command wrapper");
                    }
                    commandWrapper.StatementCompleted -= statementCompletedHandler;
                }

                lock (this)
                {
                    _state = BatchState.Initial;
                    _command.Dispose();
                    _command = null;
                }
            }

            return result;
        }

        private ScriptExecutionResult ExecuteCommand()
        {
            if (_command == null)
            {
                throw new ArgumentNullException("_command");
            }

            return this.ExecuteUnTrackedCommand();
            
            // TODO Reenable if any consumer ever needs specialized script tracking. Only Dacfx needs at present and
            // has its own copy of this code.
            //if (_isScriptExecutionTracked)
            //{
            //    return this.ExecuteTrackedCommand();
            //}
            //else
            //{
            //    return this.ExecuteUnTrackedCommand(); 
            //}
        }

        private ScriptExecutionResult ExecuteUnTrackedCommand()
        {
            IDataReader reader = null;

            if (!_isResultExpected)
            {
                _command.ExecuteNonQuery();
            }
            else
            {
                reader = _command.ExecuteReader(CommandBehavior.SequentialAccess);
            }

            return this.CheckStateAndRead(reader);
        }



        // TODO Reenable if any consumer ever needs specialized script tracking. Only Dacfx needs at present and
        // has its own copy of this code.
        //private ScriptExecutionResult ExecuteTrackedCommand()
        //{
        //    ScriptExecutionResult result = ScriptExecutionResult.Success;
        //    IDataReader reader = null;

        //    // Get the transaction retry policy
        //    RetryPolicy transactionCommandRetryPolicy = RetryPolicyFactory.PrimaryKeyViolationRetryPolicy;
            
        //    transactionCommandRetryPolicy.ExecuteAction(retryState =>
        //    {
        //        // Make sure connection is open
        //        if (_command.Connection.State != ConnectionState.Open)
        //        {
        //            _command.Connection.Open();
        //        }

        //        // Get the connection
        //        IDbConnection connection = _command.Connection;

        //        // Execute transaction
        //        bool success = false;
        //        using (IDbTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
        //        {
        //            try
        //            {
        //                // Execute the Insert statement to track this execution
        //                IDbCommand insertCommand = DeploymentScriptTracker.GetInsertCommandIntoScriptTrackingTable(connection, _scriptTrackingId);
        //                insertCommand.Transaction = transaction;
        //                insertCommand.ExecuteNonQuery();

        //                // Execute the actual deployment script
        //                _command.Transaction = transaction;
        //                if (!_isResultExpected)
        //                {
        //                    _command.ExecuteNonQuery();
        //                }
        //                else
        //                {
        //                    reader = _command.ExecuteReader(CommandBehavior.SequentialAccess);
        //                }

        //                result = this.CheckStateAndRead(reader);

        //                transaction.Commit();
        //                success = true;
        //            }
        //            finally
        //            {
        //                if (!success)
        //                {
        //                    // Attempt to rollback transaction and ignore any exceptions.
        //                    // If there was any error, we need to rollback transaction to avoid partial data being submitted.
        //                    try
        //                    {
        //                        transaction.Rollback();
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        Logger.Write(LogLevel.Error, "Exception rolling back transaction");
        //                    }
        //                }
        //            }
        //        }
        //    });

        //    return result;
        //}

        private ScriptExecutionResult CheckStateAndRead(IDataReader reader = null)
        {
            ScriptExecutionResult result = ScriptExecutionResult.Success;

            if (!_isResultExpected)
            {
                lock (this)
                {
                    if (_state == BatchState.Cancelling)
                    {
                        result = ScriptExecutionResult.Cancel;
                    }
                    else
                    {
                        result = ScriptExecutionResult.Success;
                        _state = BatchState.Executed;
                    }
                }
            }
            else
            {
                lock (this)
                {
                    if (_state == BatchState.Cancelling)
                    {
                        result = ScriptExecutionResult.Cancel;
                    }
                    else
                    {
                        _state = BatchState.ProcessingResults;
                    }
                }

                if (result != ScriptExecutionResult.Cancel)
                {
                    ScriptExecutionResult batchExecutionResult = ScriptExecutionResult.Success;

                    if (reader != null)
                    {
                        bool hasNextResult = false;
                        do
                        {
                            // if there were no results coming from the server, then the FieldCount is 0
                            if (reader.FieldCount <= 0)
                            {
                                hasNextResult = reader.NextResult();
                                continue;
                            }

                            batchExecutionResult = ProcessResultSet(reader);

                            if (batchExecutionResult != ScriptExecutionResult.Success)
                            {
                                result = batchExecutionResult;
                                break;
                            }

                            RaiseBatchResultSetFinished();

                            hasNextResult = reader.NextResult();

                        } while (hasNextResult);
                    }

                    if (_hasErrors)
                    {
                        Debug.WriteLine("DoBatchExecution: successfull processed result set, but there were errors shown to the user");
                        result = ScriptExecutionResult.Failure;
                    }

                    if (result != ScriptExecutionResult.Cancel)
                    {
                        lock (this)
                        {
                            _state = BatchState.Executed;
                        }
                    }
                }
            }

            if (reader != null)
            {
                try
                {
                    reader.Close();
                    reader = null;
                }
                catch (OutOfMemoryException)
                {
                    throw;
                }
                catch (SqlException)
                {
                    // nothing
                }
            }

            return result;
        }


        #endregion

        #region Private methods

        /// <summary>
        /// Helper method to format the provided SqlError
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        private string FormatSqlErrorMessage(SqlError error)
        {
            string detailedMessage = string.Empty;

            if (error.Class > 10)
            {
                if (String.IsNullOrEmpty(error.Procedure))
                {
                    detailedMessage = String.Format(CultureInfo.CurrentCulture, SR.EE_BatchSqlMessageNoProcedureInfo,
                            error.Number,
                            error.Class,
                            error.State,
                            error.LineNumber);
                }
                else
                {
                    detailedMessage = String.Format(CultureInfo.CurrentCulture, SR.EE_BatchSqlMessageWithProcedureInfo,
                        error.Number,
                        error.Class,
                        error.State,
                        error.Procedure,
                        error.LineNumber);
                }
            }
            else if (error.Class > 0 && error.Number > 0)
            {
                detailedMessage = String.Format(CultureInfo.CurrentCulture, SR.EE_BatchSqlMessageNoLineInfo,
                    error.Number,
                    error.Class,
                    error.State);
            }

            if (!String.IsNullOrEmpty(detailedMessage) && !_isSuppressProviderMessageHeaders)
            {
                detailedMessage = String.Format(CultureInfo.CurrentCulture, "{0}: {1}", error.Source, detailedMessage);
            }

            return detailedMessage;
        }

        /// <summary>
        /// Handles a Sql exception
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        private ScriptExecutionResult HandleSqlException(SqlException ex)
        {
            ScriptExecutionResult result;

            lock (this)
            {
                if (_state == BatchState.Cancelling)
                {
                    result = ScriptExecutionResult.Cancel;
                }
                else
                {
                    result = ScriptExecutionResult.Failure;
                }
            }

            if (result != ScriptExecutionResult.Cancel)
            {
                HandleSqlMessages(ex.Errors);
            }

            return result;
        }

        /// <summary>        
        /// Called when an error message came from SqlClient
        /// </summary>
        /// <param name="message"></param>
        /// <param name="description"></param>
        /// <param name="line"></param>
        /// <param name="textSpan"></param>
        private void RaiseBatchError(String message, SqlError error, TextSpan textSpan)
        {
            BatchErrorEventArgs args = new BatchErrorEventArgs(message, error, textSpan, null);
            RaiseBatchError(args);
        }

        /// <summary>
        /// Called when an error message came from SqlClient
        /// </summary>
        /// <param name="e"></param>
        private void RaiseBatchError(BatchErrorEventArgs e)
        {
            EventHandler<BatchErrorEventArgs> cache = BatchError;
            if (cache != null)
            {
                cache(this, e);
            }
        }

        /// <summary>
        /// Called when a message came from SqlClient
        /// </summary>
        /// <remarks>
        /// Additionally, it's being used to notify the user that the script execution
        /// has been finished.
        /// </remarks>
        /// <param name="detailedMessage"></param>
        /// <param name="message"></param>
        private void RaiseBatchMessage(String detailedMessage, String message, SqlError error)
        {
            EventHandler<BatchMessageEventArgs> cache = BatchMessage;
            if (cache != null)
            {
                BatchMessageEventArgs args = new BatchMessageEventArgs(detailedMessage, message, error);
                cache(this, args);
            }
        }

        /// <summary>
        /// Called when a new result set has to be processed
        /// </summary>
        /// <param name="resultSet"></param>
        private void RaiseBatchResultSetProcessing(IDataReader dataReader, ShowPlanType expectedShowPlan)
        {
            EventHandler<BatchResultSetEventArgs> cache = BatchResultSetProcessing;
            if (cache != null)
            {
                BatchResultSetEventArgs args = new BatchResultSetEventArgs(dataReader, expectedShowPlan);
                BatchResultSetProcessing(this, args);
            }
        }

        /// <summary>
        /// Called when the result set has been processed
        /// </summary>
        private void RaiseBatchResultSetFinished()
        {
            EventHandler<EventArgs> cache = BatchResultSetFinished;
            if (cache != null)
            {
                cache(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Called when the batch is being cancelled with an active result set 
        /// </summary>
        private void RaiseCancelling()
        {
            EventHandler<EventArgs> cache = BatchCancelling;
            if (cache != null)
            {
                cache(this, EventArgs.Empty);
            }
        }
        #endregion

        #region Private enums

        private enum BatchState
        {
            Initial,
            Executing,
            Executed,
            ProcessingResults,
            Cancelling,
        }
        #endregion

        #region Private fields

        // correspond to public properties
        private bool _isSuppressProviderMessageHeaders;
        private bool _isResultExpected = false;
        private string _sqlText = string.Empty;
        private int _execTimeout = 30;
        private int _scriptTrackingId = 0;
        private bool _isScriptExecutionTracked = false;
        private const int ChangeDatabase = 0x1645;

        //command that will be used for execution
        private IDbCommand _command = null;

        //current object state
        private BatchState _state = BatchState.Initial;

        //script text to be executed
        private TextSpan _textSpan;

        //index of the batch in collection of batches
        private int _index = 0;

        private long _totalAffectedRows = 0;

        private bool _hasErrors;

        // Expected showplan if any
        private ShowPlanType _expectedShowPlan;

        #endregion
    }
}