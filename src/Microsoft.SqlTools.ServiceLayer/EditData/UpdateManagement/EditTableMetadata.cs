﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement
{
    public class EditTableMetadata : IEditTableMetadata
    {
        private readonly List<EditColumnWrapper> columns;
        private readonly List<EditColumnWrapper> keyColumns;

        public EditTableMetadata(IList<DbColumnWrapper> dbColumns, TableViewTableTypeBase smoObject)
        {
            // Make sure that we have equal columns on both metadata providers
            Debug.Assert(dbColumns.Count == smoObject.Columns.Count);

            // Create the columns for edit usage
            columns = new List<EditColumnWrapper>();
            for (int i = 0; i < dbColumns.Count; i++)
            {
                columns.Add(new EditColumnWrapper(i, dbColumns[i], smoObject.Columns[i]));
            }

            // Determine what the key columns are
            keyColumns = columns.Where(c => c.IsKey).ToList();
            if (keyColumns.Count == 0)
            {
                // We didn't find any explicit key columns. Instead, we'll use all columns that are
                // trustworthy for uniqueness (usually all the columns)
                keyColumns = columns.Where(c => c.IsTrustworthyForUniqueness).ToList();
            }

            // If a table is memory optimized it is Hekaton. If it's a view, then it can't be Hekaton
            Table smoTable = smoObject as Table;
            IsHekaton = smoTable != null && smoTable.IsMemoryOptimized;

            // Escape the parts of the name
            string[] objectNameParts = {smoObject.Schema, smoObject.Name};
            EscapedMultipartName = SqlScriptFormatter.FormatMultipartIdentifier(objectNameParts);
        }

        public IEnumerable<IEditColumnWrapper> Columns => columns;
        public string EscapedMultipartName { get; }
        public bool IsHekaton { get; }
        public IEnumerable<IEditColumnWrapper> KeyColumns => keyColumns;
    }
}
