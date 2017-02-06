﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        /// <param name="associatedResultSet">The result set that this update will be applied to</param>
        /// <param name="associatedObject">The object (table, view, etc) that will be updated</param>
        public RowUpdate(long rowId, ResultSet associatedResultSet, string associatedObject) 
            : base(rowId, associatedResultSet, associatedObject)
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
                return $"({formattedColumnName} = {formattedValue})";
            });
            string setClause = string.Join(", ", setComponents);

            // Get the where clause
            string whereClause = GetWhereClause(false).CommandText;

            // @TODO Determine when to use Hekaton version
            // Put it all together
            return string.Format(CultureInfo.InvariantCulture, UpdateStatement, AssociatedObject, setClause, whereClause);
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
        public override string SetCell(int columnId, string newValue)
        {
            // Validate the value and convert to object
            ValidateColumnIsUpdatable(columnId);            
            CellUpdate update = new CellUpdate(AssociatedResultSet.Columns[columnId], newValue);

            // If the value is the same as the old value, we shouldn't make changes
            if (update.Value == associatedRow[columnId].RawObject)
            {
                // Remove any pending change and stop processing this
                if (cellUpdates.ContainsKey(columnId))
                {
                    cellUpdates.Remove(columnId);
                }
                return null;
            }

            // The change is real, so set it
            cellUpdates[columnId] = update;
            return update.ValueAsString;
        }
    }
}
