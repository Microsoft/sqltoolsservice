﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.SqlTools.ServiceLayer {
    using System;
    using System.Reflection;
    
    
    /// <summary>
    ///    A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class sr {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        internal sr() {
        }
        
        /// <summary>
        ///    Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.SqlTools.ServiceLayer.sr", typeof(sr).GetTypeInfo().Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///    Overrides the current thread's CurrentUICulture property for all
        ///    resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to File &apos;{0}&apos; recursively included..
        /// </summary>
        public static string BatchParser_CircularReference {
            get {
                return ResourceManager.GetString("BatchParser_CircularReference", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Missing end comment mark &apos;*/&apos;..
        /// </summary>
        public static string BatchParser_CommentNotTerminated {
            get {
                return ResourceManager.GetString("BatchParser_CommentNotTerminated", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Incorrect syntax was encountered while parsing &apos;{0}&apos;..
        /// </summary>
        public static string BatchParser_IncorrectSyntax {
            get {
                return ResourceManager.GetString("BatchParser_IncorrectSyntax", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Unclosed quotation mark after the character string..
        /// </summary>
        public static string BatchParser_StringNotTerminated {
            get {
                return ResourceManager.GetString("BatchParser_StringNotTerminated", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Variable {0} is not defined..
        /// </summary>
        public static string BatchParser_VariableNotDefined {
            get {
                return ResourceManager.GetString("BatchParser_VariableNotDefined", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Canceling batch parser wrapper batch execution..
        /// </summary>
        public static string BatchParserWrapperExecutionEngineBatchCancelling {
            get {
                return ResourceManager.GetString("BatchParserWrapperExecutionEngineBatchCancelling", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Batch parser wrapper execution engine batch message received:  Message: {0}    Detailed message: {1}.
        /// </summary>
        public static string BatchParserWrapperExecutionEngineBatchMessage {
            get {
                return ResourceManager.GetString("BatchParserWrapperExecutionEngineBatchMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Batch parser wrapper execution engine batch ResultSet finished..
        /// </summary>
        public static string BatchParserWrapperExecutionEngineBatchResultSetFinished {
            get {
                return ResourceManager.GetString("BatchParserWrapperExecutionEngineBatchResultSetFinished", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Batch parser wrapper execution engine batch ResultSet processing: DataReader.FieldCount: {0}  DataReader.RecordsAffected: {1}.
        /// </summary>
        public static string BatchParserWrapperExecutionEngineBatchResultSetProcessing {
            get {
                return ResourceManager.GetString("BatchParserWrapperExecutionEngineBatchResultSetProcessing", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to SQL Execution error: {0}.
        /// </summary>
        public static string BatchParserWrapperExecutionEngineError {
            get {
                return ResourceManager.GetString("BatchParserWrapperExecutionEngineError", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Batch parser wrapper execution: {0} found... at line {1}: {2}    Description: {3}.
        /// </summary>
        public static string BatchParserWrapperExecutionError {
            get {
                return ResourceManager.GetString("BatchParserWrapperExecutionError", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Connection details object cannot be null.
        /// </summary>
        public static string ConnectionParamsValidateNullConnection {
            get {
                return ResourceManager.GetString("ConnectionParamsValidateNullConnection", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to OwnerUri cannot be null or empty.
        /// </summary>
        public static string ConnectionParamsValidateNullOwnerUri {
            get {
                return ResourceManager.GetString("ConnectionParamsValidateNullOwnerUri", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to ServerName cannot be null or empty.
        /// </summary>
        public static string ConnectionParamsValidateNullServerName {
            get {
                return ResourceManager.GetString("ConnectionParamsValidateNullServerName", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to {0} cannot be null or empty when using SqlLogin authentication.
        /// </summary>
        public static string ConnectionParamsValidateNullSqlAuth {
            get {
                return ResourceManager.GetString("ConnectionParamsValidateNullSqlAuth", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Connection parameters cannot be null.
        /// </summary>
        public static string ConnectionServiceConnectErrorNullParams {
            get {
                return ResourceManager.GetString("ConnectionServiceConnectErrorNullParams", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Connection canceled.
        /// </summary>
        public static string ConnectionServiceConnectionCanceled {
            get {
                return ResourceManager.GetString("ConnectionServiceConnectionCanceled", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Invalid value &apos;{0}&apos; for AuthenticationType.  Valid values are &apos;Integrated&apos; and &apos;SqlLogin&apos;..
        /// </summary>
        public static string ConnectionServiceConnStringInvalidAuthType {
            get {
                return ResourceManager.GetString("ConnectionServiceConnStringInvalidAuthType", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Invalid value &apos;{0}&apos; for ApplicationIntent. Valid values are &apos;ReadWrite&apos; and &apos;ReadOnly&apos;..
        /// </summary>
        public static string ConnectionServiceConnStringInvalidIntent {
            get {
                return ResourceManager.GetString("ConnectionServiceConnStringInvalidIntent", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to SpecifiedUri &apos;{0}&apos; does not have existing connection.
        /// </summary>
        public static string ConnectionServiceListDbErrorNotConnected {
            get {
                return ResourceManager.GetString("ConnectionServiceListDbErrorNotConnected", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to OwnerUri cannot be null or empty.
        /// </summary>
        public static string ConnectionServiceListDbErrorNullOwnerUri {
            get {
                return ResourceManager.GetString("ConnectionServiceListDbErrorNullOwnerUri", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Win32Credential object is already disposed.
        /// </summary>
        public static string CredentialServiceWin32CredentialDisposed {
            get {
                return ResourceManager.GetString("CredentialServiceWin32CredentialDisposed", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Invalid CriticalHandle!.
        /// </summary>
        public static string CredentialsServiceInvalidCriticalHandle {
            get {
                return ResourceManager.GetString("CredentialsServiceInvalidCriticalHandle", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to The password has exceeded 512 bytes.
        /// </summary>
        public static string CredentialsServicePasswordLengthExceeded {
            get {
                return ResourceManager.GetString("CredentialsServicePasswordLengthExceeded", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Target must be specified to delete a credential.
        /// </summary>
        public static string CredentialsServiceTargetForDelete {
            get {
                return ResourceManager.GetString("CredentialsServiceTargetForDelete", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Target must be specified to check existance of a credential.
        /// </summary>
        public static string CredentialsServiceTargetForLookup {
            get {
                return ResourceManager.GetString("CredentialsServiceTargetForLookup", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to An error occurred while the batch was being processed. The error message is: {0}.
        /// </summary>
        public static string EE_BatchError_Exception {
            get {
                return ResourceManager.GetString("EE_BatchError_Exception", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to An error occurred while the batch was being executed..
        /// </summary>
        public static string EE_BatchExecutionError_Halting {
            get {
                return ResourceManager.GetString("EE_BatchExecutionError_Halting", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to An error occurred while the batch was being executed, but the error has been ignored..
        /// </summary>
        public static string EE_BatchExecutionError_Ignoring {
            get {
                return ResourceManager.GetString("EE_BatchExecutionError_Ignoring", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to ({0} row(s) affected).
        /// </summary>
        public static string EE_BatchExecutionInfo_RowsAffected {
            get {
                return ResourceManager.GetString("EE_BatchExecutionInfo_RowsAffected", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Msg {0}, Level {1}, State {2}.
        /// </summary>
        public static string EE_BatchSqlMessageNoLineInfo {
            get {
                return ResourceManager.GetString("EE_BatchSqlMessageNoLineInfo", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Msg {0}, Level {1}, State {2}, Line {3}.
        /// </summary>
        public static string EE_BatchSqlMessageNoProcedureInfo {
            get {
                return ResourceManager.GetString("EE_BatchSqlMessageNoProcedureInfo", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Msg {0}, Level {1}, State {2}, Procedure {3}, Line {4}.
        /// </summary>
        public static string EE_BatchSqlMessageWithProcedureInfo {
            get {
                return ResourceManager.GetString("EE_BatchSqlMessageWithProcedureInfo", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Command {0} is not supported..
        /// </summary>
        public static string EE_ExecutionError_CommandNotSupported {
            get {
                return ResourceManager.GetString("EE_ExecutionError_CommandNotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to The variable {0} could not be found..
        /// </summary>
        public static string EE_ExecutionError_VariableNotFound {
            get {
                return ResourceManager.GetString("EE_ExecutionError_VariableNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Execution completed {0} times....
        /// </summary>
        public static string EE_ExecutionInfo_FinalizingLoop {
            get {
                return ResourceManager.GetString("EE_ExecutionInfo_FinalizingLoop", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Starting execution loop of {0} times....
        /// </summary>
        public static string EE_ExecutionInfo_InitilizingLoop {
            get {
                return ResourceManager.GetString("EE_ExecutionInfo_InitilizingLoop", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to You cancelled the query..
        /// </summary>
        public static string EE_ExecutionInfo_QueryCancelledbyUser {
            get {
                return ResourceManager.GetString("EE_ExecutionInfo_QueryCancelledbyUser", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to The previous execution is not yet complete..
        /// </summary>
        public static string EE_ExecutionNotYetCompleteError {
            get {
                return ResourceManager.GetString("EE_ExecutionNotYetCompleteError", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to A scripting error occurred..
        /// </summary>
        public static string EE_ScriptError_Error {
            get {
                return ResourceManager.GetString("EE_ScriptError_Error", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to A fatal error occurred..
        /// </summary>
        public static string EE_ScriptError_FatalError {
            get {
                return ResourceManager.GetString("EE_ScriptError_FatalError", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Incorrect syntax was encountered while {0} was being parsed..
        /// </summary>
        public static string EE_ScriptError_ParsingSyntax {
            get {
                return ResourceManager.GetString("EE_ScriptError_ParsingSyntax", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Scripting warning..
        /// </summary>
        public static string EE_ScriptError_Warning {
            get {
                return ResourceManager.GetString("EE_ScriptError_Warning", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Message header must separate key and value using &apos;:&apos;.
        /// </summary>
        public static string HostingHeaderMissingColon {
            get {
                return ResourceManager.GetString("HostingHeaderMissingColon", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Fatal error: Content-Length header must be provided.
        /// </summary>
        public static string HostingHeaderMissingContentLengthHeader {
            get {
                return ResourceManager.GetString("HostingHeaderMissingContentLengthHeader", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Fatal error: Content-Length value is not an integer.
        /// </summary>
        public static string HostingHeaderMissingContentLengthValue {
            get {
                return ResourceManager.GetString("HostingHeaderMissingContentLengthValue", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to MessageReader&apos;s input stream ended unexpectedly, terminating.
        /// </summary>
        public static string HostingUnexpectedEndOfStream {
            get {
                return ResourceManager.GetString("HostingUnexpectedEndOfStream", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to This feature is currently not supported on Azure SQL DB and Data Warehouse: {0}.
        /// </summary>
        public static string PeekDefinitionAzureError {
            get {
                return ResourceManager.GetString("PeekDefinitionAzureError", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to No database object was retrieved..
        /// </summary>
        public static string PeekDefinitionDatabaseError {
            get {
                return ResourceManager.GetString("PeekDefinitionDatabaseError", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to An unexpected error occurred during Peek Definition execution: {0}.
        /// </summary>
        public static string PeekDefinitionError {
            get {
                return ResourceManager.GetString("PeekDefinitionError", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to No results were found..
        /// </summary>
        public static string PeekDefinitionNoResultsError {
            get {
                return ResourceManager.GetString("PeekDefinitionNoResultsError", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Please connect to a server..
        /// </summary>
        public static string PeekDefinitionNotConnectedError {
            get {
                return ResourceManager.GetString("PeekDefinitionNotConnectedError", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Operation timed out..
        /// </summary>
        public static string PeekDefinitionTimedoutError {
            get {
                return ResourceManager.GetString("PeekDefinitionTimedoutError", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to This object type is currently not supported by this feature..
        /// </summary>
        public static string PeekDefinitionTypeNotSupportedError {
            get {
                return ResourceManager.GetString("PeekDefinitionTypeNotSupportedError", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to (1 row affected).
        /// </summary>
        public static string QueryServiceAffectedOneRow {
            get {
                return ResourceManager.GetString("QueryServiceAffectedOneRow", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to ({0} rows affected).
        /// </summary>
        public static string QueryServiceAffectedRows {
            get {
                return ResourceManager.GetString("QueryServiceAffectedRows", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to The query has already completed, it cannot be cancelled.
        /// </summary>
        public static string QueryServiceCancelAlreadyCompleted {
            get {
                return ResourceManager.GetString("QueryServiceCancelAlreadyCompleted", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Query successfully cancelled, failed to dispose query. Owner URI not found..
        /// </summary>
        public static string QueryServiceCancelDisposeFailed {
            get {
                return ResourceManager.GetString("QueryServiceCancelDisposeFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to (No column name).
        /// </summary>
        public static string QueryServiceColumnNull {
            get {
                return ResourceManager.GetString("QueryServiceColumnNull", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Commands completed successfully..
        /// </summary>
        public static string QueryServiceCompletedSuccessfully {
            get {
                return ResourceManager.GetString("QueryServiceCompletedSuccessfully", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Maximum number of bytes to return must be greater than zero.
        /// </summary>
        public static string QueryServiceDataReaderByteCountInvalid {
            get {
                return ResourceManager.GetString("QueryServiceDataReaderByteCountInvalid", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Maximum number of chars to return must be greater than zero.
        /// </summary>
        public static string QueryServiceDataReaderCharCountInvalid {
            get {
                return ResourceManager.GetString("QueryServiceDataReaderCharCountInvalid", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Maximum number of XML bytes to return must be greater than zero.
        /// </summary>
        public static string QueryServiceDataReaderXmlCountInvalid {
            get {
                return ResourceManager.GetString("QueryServiceDataReaderXmlCountInvalid", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Msg {0}, Level {1}, State {2}, Line {3}{4}{5}.
        /// </summary>
        public static string QueryServiceErrorFormat {
            get {
                return ResourceManager.GetString("QueryServiceErrorFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Could not retrieve an execution plan from the result set .
        /// </summary>
        public static string QueryServiceExecutionPlanNotFound {
            get {
                return ResourceManager.GetString("QueryServiceExecutionPlanNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to FileStreamWrapper must be initialized before performing operations.
        /// </summary>
        public static string QueryServiceFileWrapperNotInitialized {
            get {
                return ResourceManager.GetString("QueryServiceFileWrapperNotInitialized", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to This FileStreamWrapper cannot be used for writing.
        /// </summary>
        public static string QueryServiceFileWrapperReadOnly {
            get {
                return ResourceManager.GetString("QueryServiceFileWrapperReadOnly", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Access method cannot be write-only.
        /// </summary>
        public static string QueryServiceFileWrapperWriteOnly {
            get {
                return ResourceManager.GetString("QueryServiceFileWrapperWriteOnly", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Sender for OnInfoMessage event must be a SqlConnection.
        /// </summary>
        public static string QueryServiceMessageSenderNotSql {
            get {
                return ResourceManager.GetString("QueryServiceMessageSenderNotSql", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Query was canceled by user.
        /// </summary>
        public static string QueryServiceQueryCancelled {
            get {
                return ResourceManager.GetString("QueryServiceQueryCancelled", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Query failed: {0}.
        /// </summary>
        public static string QueryServiceQueryFailed {
            get {
                return ResourceManager.GetString("QueryServiceQueryFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to A query is already in progress for this editor session. Please cancel this query or wait for its completion..
        /// </summary>
        public static string QueryServiceQueryInProgress {
            get {
                return ResourceManager.GetString("QueryServiceQueryInProgress", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to This editor is not connected to a database.
        /// </summary>
        public static string QueryServiceQueryInvalidOwnerUri {
            get {
                return ResourceManager.GetString("QueryServiceQueryInvalidOwnerUri", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to The requested query does not exist.
        /// </summary>
        public static string QueryServiceRequestsNoQuery {
            get {
                return ResourceManager.GetString("QueryServiceRequestsNoQuery", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Could not retrieve column schema for result set.
        /// </summary>
        public static string QueryServiceResultSetNoColumnSchema {
            get {
                return ResourceManager.GetString("QueryServiceResultSetNoColumnSchema", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Cannot read subset unless the results have been read from the server.
        /// </summary>
        public static string QueryServiceResultSetNotRead {
            get {
                return ResourceManager.GetString("QueryServiceResultSetNotRead", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Reader cannot be null.
        /// </summary>
        public static string QueryServiceResultSetReaderNull {
            get {
                return ResourceManager.GetString("QueryServiceResultSetReaderNull", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Row count must be a positive integer.
        /// </summary>
        public static string QueryServiceResultSetRowCountOutOfRange {
            get {
                return ResourceManager.GetString("QueryServiceResultSetRowCountOutOfRange", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Start row cannot be less than 0 or greater than the number of rows in the result set.
        /// </summary>
        public static string QueryServiceResultSetStartRowOutOfRange {
            get {
                return ResourceManager.GetString("QueryServiceResultSetStartRowOutOfRange", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Failed to save {0}: {1}.
        /// </summary>
        public static string QueryServiceSaveAsFail {
            get {
                return ResourceManager.GetString("QueryServiceSaveAsFail", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to A save request to the same path is in progress.
        /// </summary>
        public static string QueryServiceSaveAsInProgress {
            get {
                return ResourceManager.GetString("QueryServiceSaveAsInProgress", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Internal error occurred while starting save task.
        /// </summary>
        public static string QueryServiceSaveAsMiscStartingError {
            get {
                return ResourceManager.GetString("QueryServiceSaveAsMiscStartingError", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Result cannot be saved until query execution has completed.
        /// </summary>
        public static string QueryServiceSaveAsResultSetNotComplete {
            get {
                return ResourceManager.GetString("QueryServiceSaveAsResultSetNotComplete", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to The batch has not completed, yet.
        /// </summary>
        public static string QueryServiceSubsetBatchNotCompleted {
            get {
                return ResourceManager.GetString("QueryServiceSubsetBatchNotCompleted", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Batch index cannot be less than 0 or greater than the number of batches.
        /// </summary>
        public static string QueryServiceSubsetBatchOutOfRange {
            get {
                return ResourceManager.GetString("QueryServiceSubsetBatchOutOfRange", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Result set index cannot be less than 0 or greater than the number of result sets.
        /// </summary>
        public static string QueryServiceSubsetResultSetOutOfRange {
            get {
                return ResourceManager.GetString("QueryServiceSubsetResultSetOutOfRange", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to EN_LOCALIZATION.
        /// </summary>
        public static string TestLocalizationConstant {
            get {
                return ResourceManager.GetString("TestLocalizationConstant", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to For more information about this error, see the troubleshooting topics in the product documentation..
        /// </summary>
        public static string TroubleshootingAssistanceMessage {
            get {
                return ResourceManager.GetString("TroubleshootingAssistanceMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Start position ({0}, {1}) must come before or be equal to the end position ({2}, {3}).
        /// </summary>
        public static string WorkspaceServiceBufferPositionOutOfOrder {
            get {
                return ResourceManager.GetString("WorkspaceServiceBufferPositionOutOfOrder", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Position is outside of column range for line {0}.
        /// </summary>
        public static string WorkspaceServicePositionColumnOutOfRange {
            get {
                return ResourceManager.GetString("WorkspaceServicePositionColumnOutOfRange", resourceCulture);
            }
        }
        
        /// <summary>
        ///    Looks up a localized string similar to Position is outside of file line range.
        /// </summary>
        public static string WorkspaceServicePositionLineOutOfRange {
            get {
                return ResourceManager.GetString("WorkspaceServicePositionLineOutOfRange", resourceCulture);
            }
        }
    }
}
