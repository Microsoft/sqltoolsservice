﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.Common;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.EditData.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;

namespace Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement
{
    /// <summary>
    /// Represents a row that should be deleted. This will generate a DELETE statement
    /// </summary>
    public sealed class RowDelete : RowEditBase
    {
        private const string DeleteStatement = "DELETE FROM {0} {1}";
        private const string DeleteMemoryOptimizedStatement = "DELETE FROM {0} WITH(SNAPSHOT) {1}";

        /// <summary>
        /// Constructs a new RowDelete object
        /// </summary>
        /// <param name="rowId">Internal ID of the row to be deleted</param>
        /// <param name="associatedResultSet">Result set that is being edited</param>
        /// <param name="associatedMetadata">Improved metadata of the object being edited</param>
        public RowDelete(long rowId, ResultSet associatedResultSet, IEditTableMetadata associatedMetadata)
            : base(rowId, associatedResultSet, associatedMetadata)
        {
        }

        protected override int SortId => 2;

        public override Task ApplyChanges(DbDataReader dataReader)
        {
            // Take the result set and remove the row from it
            AssociatedResultSet.RemoveRow(RowId);
            return Task.FromResult(0);
        }

        /// <summary>
        /// Generates a command for deleting the selected row
        /// </summary>
        /// <returns></returns>
        public override DbCommand GetCommand(DbConnection connection)
        {
            // Return a SqlCommand with formatted with the parameters from the where clause
            WhereClause where = GetWhereClause(true);
            string commandText = GetCommandText(where.CommandText);

            DbCommand command = connection.CreateCommand();
            command.CommandText = commandText;
            command.Parameters.AddRange(where.Parameters.ToArray());

            return command;
        }

        /// <summary>
        /// Generates a DELETE statement to delete this row
        /// </summary>
        /// <returns>String of the DELETE statement</returns>
        public override string GetScript()
        {
            return GetCommandText(GetWhereClause(false).CommandText);
        }

        /// <summary>
        /// This method should not be called. A cell cannot be updated on a row that is pending
        /// deletion.
        /// </summary>
        /// <exception cref="InvalidOperationException">Always thrown</exception>
        /// <param name="columnId">Ordinal of the column to update</param>
        /// <param name="newValue">New value for the cell</param>
        public override EditUpdateCellResult SetCell(int columnId, string newValue)
        {
            throw new InvalidOperationException(SR.EditDataDeleteSetCell);
        }

        protected override int CompareToSameType(RowEditBase rowEdit)
        {
            // We want to sort by row ID *IN REVERSE* to make sure we delete from the bottom first.
            // If we delete from the top first, it will change IDs, making all subsequent deletes
            // off by one or more!
            return RowId.CompareTo(rowEdit.RowId) * -1;
        }

        private string GetCommandText(string whereText)
        {
            string formatString = AssociatedObjectMetadata.IsMemoryOptimized
                ? DeleteMemoryOptimizedStatement
                : DeleteStatement;

            return string.Format(CultureInfo.InvariantCulture, formatString,
                AssociatedObjectMetadata.EscapedMultipartName, whereText);
        }
    }
}
