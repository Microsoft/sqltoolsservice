﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.EditData.Contracts;
using Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.EditData
{
    /// <summary>
    /// Represents an edit "session" bound to the results of a query, containing a cache of edits
    /// that are pending. Provides logic for performing edit operations.
    /// </summary>
    public class Session
    {

        private readonly ResultSet associatedResultSet;
        private readonly IEditTableMetadata objectMetadata;

        /// <summary>
        /// Constructs a new edit session bound to the result set and metadat object provided
        /// </summary>
        /// <param name="resultSet">The result set of the table to be edited</param>
        /// <param name="objMetadata">Metadata provider for the table to be edited</param>
        public Session(ResultSet resultSet, IEditTableMetadata objMetadata)
        {
            Validate.IsNotNull(nameof(resultSet), resultSet);
            Validate.IsNotNull(nameof(objMetadata), objMetadata);

            // Setup the internal state
            associatedResultSet = resultSet;
            objectMetadata = objMetadata;
            NextRowId = associatedResultSet.RowCount;
            EditCache = new ConcurrentDictionary<long, RowEditBase>();
        }

        #region Properties

        internal Task CommitTask { get; private set; }

        /// <summary>
        /// The internal ID for the next row in the table. Internal for unit testing purposes only.
        /// </summary>
        internal long NextRowId { get; private set; }

        /// <summary>
        /// The cache of pending updates. Internal for unit test purposes only
        /// </summary>
        internal ConcurrentDictionary<long, RowEditBase> EditCache { get; }

        #endregion

        #region Public Methods

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
        public long CreateRow()
        {
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

            return newRowId;
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
            Validate.IsNotNull(nameof(connection), connection);
            Validate.IsNotNull(nameof(successHandler), successHandler);
            Validate.IsNotNull(nameof(errorHandler), errorHandler);

            // Make sure that there a commit task isn't in progress
            if (CommitTask != null && !CommitTask.IsCompleted)
            {
                // @TODO: Move to constants file
                errorHandler(new InvalidOperationException("A commit task is in progress. Please wait for completion."));
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
        /// Removes a pending row update from the update cache.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If a pending row update with the given row ID does not exist.
        /// </exception>
        /// <param name="rowId">The internal ID of the row to reset</param>
        public void RevertRow(long rowId)
        {
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

        #region Private Helpers

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

        #endregion
    }
}
