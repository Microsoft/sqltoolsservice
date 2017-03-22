﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.EditData.Contracts;
using Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.EditData
{
    /// <summary>
    /// Represents an edit "session" bound to the results of a query, containing a cache of edits
    /// that are pending. Provides logic for performing edit operations.
    /// </summary>
    public class EditSession
    {

        private ResultSet associatedResultSet;

        private readonly IEditMetadataFactory metadataFactory;
        private EditTableMetadata objectMetadata;
        private readonly string objectName;
        private readonly string objectType;

        /// <summary>
        /// Constructs a new edit session bound to the result set and metadat object provided
        /// </summary>
        /// <param name="metaFactory">Factory for creating metadata</param>
        /// <param name="objName">The name of the object to edit</param>
        /// <param name="objType">The type of the object to edit</param>
        public EditSession(IEditMetadataFactory metaFactory, string objName, string objType)
        {
            Validate.IsNotNull(nameof(metaFactory), metaFactory);
            Validate.IsNotNullOrWhitespaceString(nameof(objName), objName);
            Validate.IsNotNullOrWhitespaceString(nameof(objType), objType);

            // Setup the internal state
            metadataFactory = metaFactory;
            objectName = objName;
            objectType = objType;
        }

        #region Properties

        public delegate Task<DbConnection> Connector();

        public delegate Task<EditSessionQueryExecutionState> QueryRunner(string query);

        /// <summary>
        /// The task that is running to commit the changes to the db
        /// Internal for unit test purposes.
        /// </summary>
        internal Task CommitTask { get; set; }

        /// <summary>
        /// The internal ID for the next row in the table. Internal for unit testing purposes only.
        /// </summary>
        internal long NextRowId { get; private set; }

        /// <summary>
        /// The cache of pending updates. Internal for unit test purposes only
        /// </summary>
        internal ConcurrentDictionary<long, RowEditBase> EditCache { get; private set; }

        /// <summary>
        /// The task that is running to initialize the edit session
        /// </summary>
        internal Task InitializeTask { get; set; }

        /// <summary>
        /// Whether or not the session has been initialized
        /// </summary>
        public bool IsInitialized { get; internal set; }

        #endregion

        #region Public Methods

        public void Initialize(Connector connector, QueryRunner queryRunner, Func<Task> successHandler, Func<Exception, Task> errorHandler)
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException(SR.EditDataSessionAlreadyInitialized);
            }

            if (InitializeTask != null)
            {
                throw new InvalidOperationException(SR.EditDataSessionAlreadyInitializing);
            }

            Validate.IsNotNull(nameof(connector), connector);
            Validate.IsNotNull(nameof(queryRunner), queryRunner);
            Validate.IsNotNull(nameof(successHandler), successHandler);
            Validate.IsNotNull(nameof(errorHandler), errorHandler);

            // Start up the initialize process
            InitializeTask = InitializeInternal(connector, queryRunner, successHandler, errorHandler);
        }

        /// <summary>
        /// Validates that a query can be used for an edit session. The target result set is returned
        /// </summary>
        /// <param name="query">The query to validate</param>
        /// <returns>The result set to use</returns>
        public static ResultSet ValidateQueryForSession(Query query)
        {
            Validate.IsNotNull(nameof(query), query);

            // Determine if the query is valid for editing
            // Criterion 1) Query has finished executing
            if (!query.HasExecuted)
            {
                throw new InvalidOperationException(SR.EditDataQueryNotCompleted);
            }

            // Criterion 2) Query only has a single result set
            ResultSet[] queryResultSets = query.Batches.SelectMany(b => b.ResultSets).ToArray();
            if (queryResultSets.Length != 1)
            {
                throw new InvalidOperationException(SR.EditDataQueryImproperResultSets);
            }

            return query.Batches[0].ResultSets[0];
        }

        /// <summary>
        /// Creates a new row update and adds it to the update cache
        /// </summary>
        /// <exception cref="InvalidOperationException">If inserting into cache fails</exception>
        /// <returns>The internal ID of the newly created row</returns>
        public EditCreateRowResult CreateRow()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException(SR.EditDataSessionNotInitialized);
            }

            // Create a new row ID (atomically, since this could be accesses concurrently)
            long newRowId = NextRowId++;

            // Create a new row create update and add to the update cache
            RowCreate newRow = new RowCreate(newRowId, associatedResultSet, objectMetadata);
            if (!EditCache.TryAdd(newRowId, newRow))
            {
                // Revert the next row ID
                NextRowId--;
                throw new InvalidOperationException(SR.EditDataFailedAddRow);
            }

            // Set the default values of the row if we know them
            string[] defaultValues = new string[objectMetadata.Columns.Length];
            for(int i = 0; i < objectMetadata.Columns.Length; i++)
            {
                EditColumnMetadata col = objectMetadata.Columns[i];

                // If the column is calculated, return the calculated placeholder as the display value
                if (col.IsCalculated.HasTrue())
                {
                    defaultValues[i] = SR.EditDataComputedColumnPlaceholder;
                }
                else
                {
                    if (col.DefaultValue != null)
                    {
                        newRow.SetCell(i, col.DefaultValue);
                    }
                    defaultValues[i] = col.DefaultValue;
                }
            }

            EditCreateRowResult output = new EditCreateRowResult
            {
                NewRowId = newRowId,
                DefaultValues = defaultValues
            };
            return output;
        }

        /// <summary>
        /// Commits the edits in the cache to the database and then to the associated result set of
        /// this edit session. This is launched asynchronously.
        /// </summary>
        /// <param name="connection">The connection to use for executing the query</param>
        /// <param name="successHandler">Callback to perform when the commit process has finished</param>
        /// <param name="errorHandler">Callback to perform if the commit process has failed at some point</param>
        public void CommitEdits(DbConnection connection, Func<Task> successHandler, Func<Exception, Task> errorHandler)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException(SR.EditDataSessionNotInitialized);
            }

            Validate.IsNotNull(nameof(connection), connection);
            Validate.IsNotNull(nameof(successHandler), successHandler);
            Validate.IsNotNull(nameof(errorHandler), errorHandler);

            // Make sure that there isn't a commit task in progress
            if (CommitTask != null && !CommitTask.IsCompleted)
            {
                throw new InvalidOperationException(SR.EditDataCommitInProgress);
            }

            // Start up the commit process
            CommitTask = CommitEditsInternal(connection, successHandler, errorHandler);
        }

        /// <summary>
        /// Creates a delete row update and adds it to the update cache
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// If row requested to delete already has a pending change in the cache
        /// </exception>
        /// <param name="rowId">The internal ID of the row to delete</param>
        public void DeleteRow(long rowId)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException(SR.EditDataSessionNotInitialized);
            }

            // Sanity check the row ID
            if (rowId >= NextRowId || rowId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rowId), SR.EditDataRowOutOfRange);
            }

            // Create a new row delete update and add to cache
            RowDelete deleteRow = new RowDelete(rowId, associatedResultSet, objectMetadata);
            if (!EditCache.TryAdd(rowId, deleteRow))
            {
                throw new InvalidOperationException(SR.EditDataUpdatePending);
            }
        }

        /// <summary>
        /// Retrieves a subset of rows with the pending updates applied. If more rows than exist
        /// are requested, only the rows that exist will be returned.
        /// </summary>
        /// <param name="startIndex">Index to start returning rows from</param>
        /// <param name="rowCount">The number of rows to return.</param>
        /// <returns>An array of rows with pending edits applied</returns>
        public async Task<EditRow[]> GetRows(long startIndex, int rowCount)
        {
            // Get the cached rows from the result set
            ResultSetSubset cachedRows = startIndex < associatedResultSet.RowCount
                ? await associatedResultSet.GetSubset(startIndex, rowCount)
                : new ResultSetSubset
                {
                    RowCount = 0,
                    Rows = new DbCellValue[][] { }
                };

            // Convert the rows into EditRows and apply the changes we have
            List<EditRow> editRows = new List<EditRow>();
            for (int i = 0; i < cachedRows.RowCount; i++)
            {
                long rowId = i + startIndex;
                RowEditBase edr;
                if (EditCache.TryGetValue(rowId, out edr))
                {
                    // Ask the edit object to generate an edit row
                    editRows.Add(edr.GetEditRow(cachedRows.Rows[i]));
                }
                else
                {
                    // Package up the existing row into a clean edit row
                    EditRow er = new EditRow
                    {
                        Id = rowId,
                        Cells = cachedRows.Rows[i],
                        State = EditRow.EditRowState.Clean
                    };
                    editRows.Add(er);
                }
            }

            // If the requested range of rows was at the end of the original cell set and we have
            // added new rows, we need to reflect those changes
            if (rowCount > cachedRows.RowCount)
            {
                long endIndex = startIndex + cachedRows.RowCount;
                var newRows = EditCache.Where(edit => edit.Key >= endIndex).Take(rowCount - cachedRows.RowCount);
                editRows.AddRange(newRows.Select(newRow => newRow.Value.GetEditRow(null)));
            }

            return editRows.ToArray();
        }

        /// <summary>
        /// Reverts a cell in a pending edit
        /// </summary>
        /// <param name="rowId">Internal ID of the row to have its edits reverted</param>
        /// <param name="columnId">Ordinal ID of the column to revert</param>
        /// <returns>String version of the old value for the cell</returns>
        public string RevertCell(long rowId, int columnId)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException(SR.EditDataSessionNotInitialized);
            }

            // Attempt to get the row edit with the given ID
            RowEditBase pendingEdit;
            if (!EditCache.TryGetValue(rowId, out pendingEdit))
            {
                throw new ArgumentOutOfRangeException(nameof(rowId), SR.EditDataUpdateNotPending);
            }

            // Have the edit base revert the cell
            return pendingEdit.RevertCell(columnId);
        }

        /// <summary>
        /// Removes a pending row update from the update cache.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If a pending row update with the given row ID does not exist.
        /// </exception>
        /// <param name="rowId">The internal ID of the row to reset</param>
        public void RevertRow(long rowId)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException(SR.EditDataSessionNotInitialized);
            }

            // Attempt to remove the row with the given ID
            RowEditBase removedEdit;
            if (!EditCache.TryRemove(rowId, out removedEdit))
            {
                throw new ArgumentOutOfRangeException(nameof(rowId), SR.EditDataUpdateNotPending);
            }
        }

        /// <summary>
        /// Generates a single script file with all the pending edits scripted.
        /// </summary>
        /// <param name="outputPath">The path to output the script to</param>
        /// <returns></returns>
        public string ScriptEdits(string outputPath)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException(SR.EditDataSessionNotInitialized);
            }

            // Validate the output path
            // @TODO: Reinstate this code once we have an interface around file generation
            //if (outputPath == null)
            //{
            //    // If output path isn't provided, we'll use a temporary location
            //    outputPath = Path.GetTempFileName();
            //}
            //else 
            if (outputPath == null || outputPath.Trim() == string.Empty)
            {
                // If output path is empty, that's an error
                throw new ArgumentNullException(nameof(outputPath), SR.EditDataScriptFilePathNull);
            }

            // Open a handle to the output file
            using (FileStream outputStream = File.OpenWrite(outputPath))
            using (TextWriter outputWriter = new StreamWriter(outputStream))
            {

                // Convert each update in the cache into an insert/update/delete statement
                foreach (RowEditBase rowEdit in EditCache.Values)
                {
                    outputWriter.WriteLine(rowEdit.GetScript());
                }
            }

            // Return the location of the generated script
            return outputPath;
        }

        /// <summary>
        /// Performs an update to a specific cell in a row. If the row has not already been
        /// initialized with a record in the update cache, one is created.
        /// </summary>
        /// <exception cref="InvalidOperationException">If adding a new update row fails</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If the row that is requested to be edited is beyond the rows in the results and the
        /// rows that are being added.
        /// </exception>
        /// <param name="rowId">The internal ID of the row to edit</param>
        /// <param name="columnId">The ordinal of the column to edit in the row</param>
        /// <param name="newValue">The new string value of the cell to update</param>
        public EditUpdateCellResult UpdateCell(long rowId, int columnId, string newValue)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException(SR.EditDataSessionNotInitialized);
            }

            // Sanity check to make sure that the row ID is in the range of possible values
            if (rowId >= NextRowId || rowId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rowId), SR.EditDataRowOutOfRange);
            }

            // Attempt to get the row that is being edited, create a new update object if one
            // doesn't exist
            // NOTE: This *must* be done as a lambda. RowUpdate creation requires that the row
            // exist in the result set. We only want a new RowUpdate to be created if the edit
            // doesn't already exist in the cache
            RowEditBase editRow = EditCache.GetOrAdd(rowId, key => new RowUpdate(rowId, associatedResultSet, objectMetadata));

            // Pass the call to the row update
            return editRow.SetCell(columnId, newValue);
        }

        #endregion

        private async Task InitializeInternal(Connector connector, QueryRunner queryRunner,
            Func<Task> successHandler, Func<Exception, Task> failureHandler)
        {
            try
            {
                // Step 1) Look up the SMO metadata
                objectMetadata = metadataFactory.GetObjectMetadata(await connector(), objectName, objectType);

                // Step 2) Get and execute a query for the rows in the object we're looking up
                EditSessionQueryExecutionState state = await queryRunner(ConstructInitializeQuery());
                if (state.Query == null)
                {
                    // TODO: Move to SR file
                    string message = state.Message ?? SR.EditDataQueryFailed;
                    throw new Exception(message);
                }

                // Step 3) Setup the internal state
                associatedResultSet = ValidateQueryForSession(state.Query);
                NextRowId = associatedResultSet.RowCount;
                EditCache = new ConcurrentDictionary<long, RowEditBase>();
                IsInitialized = true;

                // Step 4) Return our success
                await successHandler();
            }
            catch (Exception e)
            {
                await failureHandler(e);
            }
        }

        private async Task CommitEditsInternal(DbConnection connection, Func<Task> successHandler, Func<Exception, Task> errorHandler)
        {
            try
            {
                // @TODO: Add support for transactional commits
             
                // Trust the RowEdit to sort itself appropriately
                var editOperations = EditCache.Values.ToList();
                editOperations.Sort();
                foreach (var editOperation in editOperations)
                {
                    // Get the command from the edit operation and execute it
                    using (DbCommand editCommand = editOperation.GetCommand(connection))
                    using (DbDataReader reader = await editCommand.ExecuteReaderAsync())
                    {
                        // Apply the changes of the command to the result set
                        await editOperation.ApplyChanges(reader);
                    }

                    // If we succeeded in applying the changes, then remove this from the cache
                    // @TODO: Prevent edit sessions from being modified while a commit is in progress
                    RowEditBase re;
                    EditCache.TryRemove(editOperation.RowId, out re);
                }
                await successHandler();
            }
            catch (Exception e)
            {
                await errorHandler(e);
            }
        }

        private string ConstructInitializeQuery()
        {
            // Using the columns we know, put together a query for the rows in the table
            var columns = objectMetadata.Columns.Select(col => col.EscapedName);
            var columnClause = string.Join(", ", columns);

            return $"SELECT ${columnClause} FROM ${objectMetadata.EscapedMultipartName}";
        }

        public class EditSessionQueryExecutionState
        {
            public Query Query { get; set; }
            public string Message { get; set; }

            public EditSessionQueryExecutionState(Query query, string message = null)
            {
                Query = query;
                Message = message;
            }
        }
    }
}
