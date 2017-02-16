﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.EditData.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement
{
    /// <summary>
    /// An update to apply to a row of a result set. This will generate an UPDATE statement.
    /// </summary>
    public sealed class RowUpdate : RowEditBase
    {
        private const string UpdateStatement = "UPDATE {0} SET {1} {2}";
        private const string UpdateStatementHekaton = "UPDATE {0} WITH (SNAPSHOT) SET {1} {2}";

        private readonly Dictionary<int, CellUpdate> cellUpdates;
        private readonly IList<DbCellValue> associatedRow;

        /// <summary>
        /// Constructs a new RowUpdate to be added to the cache.
        /// </summary>
        /// <param name="rowId">Internal ID of the row that will be updated with this object</param>
        /// <param name="associatedResultSet">Result set for the rows of the object to update</param>
        /// <param name="associatedMetadata">Metadata provider for the object to update</param>
        public RowUpdate(long rowId, ResultSet associatedResultSet, IEditTableMetadata associatedMetadata)
            : base(rowId, associatedResultSet, associatedMetadata)
        {
            cellUpdates = new Dictionary<int, CellUpdate>();
            associatedRow = associatedResultSet.GetRow(rowId);
        }

        /// <summary>
        /// Constructs an update statement to change the associated row.
        /// </summary>
        /// <returns>An UPDATE statement</returns>
        public override string GetScript()
        {
            // Build the "SET" portion of the statement
            IEnumerable<string> setComponents = cellUpdates.Select(kvp =>
            {
                string formattedColumnName = SqlScriptFormatter.FormatIdentifier(kvp.Value.Column.ColumnName);
                string formattedValue = SqlScriptFormatter.FormatValue(kvp.Value.Value, kvp.Value.Column);
                return $"{formattedColumnName} = {formattedValue}";
            });
            string setClause = string.Join(", ", setComponents);

            // Get the where clause
            string whereClause = GetWhereClause(false).CommandText;

            // Put it all together
            string formatString = AssociatedObjectMetadata.IsHekaton ? UpdateStatementHekaton : UpdateStatement;
            return string.Format(CultureInfo.InvariantCulture, formatString,
                AssociatedObjectMetadata.EscapedMultipartName, setClause, whereClause);
        }

        /// <summary>
        /// Sets the value of the cell in the associated row. If <paramref name="newValue"/> is
        /// identical to the original value, this will remove the cell update from the row update.
        /// </summary>
        /// <param name="columnId">Ordinal of the columns that will be set</param>
        /// <param name="newValue">String representation of the value the user input</param>
        /// <returns>
        /// The string representation of the new value (after conversion to target object) if the
        /// a change is made. <c>null</c> is returned if the cell is reverted to it's original value.
        /// </returns>
        public override EditUpdateCellResult SetCell(int columnId, string newValue)
        {
            // Validate the value and convert to object
            ValidateColumnIsUpdatable(columnId);            
            CellUpdate update = new CellUpdate(AssociatedResultSet.Columns[columnId], newValue);

            // If the value is the same as the old value, we shouldn't make changes
            // NOTE: We must use .Equals in order to ignore object to object comparisons
            if (update.Value.Equals(associatedRow[columnId].RawObject))
            {
                // Remove any pending change and stop processing this
                if (cellUpdates.ContainsKey(columnId))
                {
                    cellUpdates.Remove(columnId);
                }
                return new EditUpdateCellResult
                {
                    HasCorrections = false,
                    NewValue = associatedRow[columnId].DisplayValue,
                    IsRevert = true,
                    IsNull = associatedRow[columnId].IsNull
                };
            }

            // The change is real, so set it
            cellUpdates[columnId] = update;
            return new EditUpdateCellResult
            {
                HasCorrections = update.ValueAsString != newValue,
                NewValue = update.ValueAsString != newValue ? update.ValueAsString : null,
                IsNull = update.Value == DBNull.Value,
                IsRevert = false            // If we're in this branch, it is not a revert
            };
        }
    }
}
