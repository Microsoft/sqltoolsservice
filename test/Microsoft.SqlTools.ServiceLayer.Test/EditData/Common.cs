﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Moq;

namespace Microsoft.SqlTools.ServiceLayer.Test.EditData
{
    public class Common
    {
        public static IEditTableMetadata GetMetadata(DbColumn[] columns, bool allKeys = true, bool isHekaton = false)
        {
            // Create a Column Metadata Provider
            var columnMetas = columns.Select((c, i) =>
            {
                var columnMetaMock = new Mock<IEditColumnWrapper>();
                columnMetaMock.Setup(m => m.DbColumn).Returns(new DbColumnWrapper(c));
                columnMetaMock.Setup(m => m.Ordinal).Returns(i);
                columnMetaMock.Setup(m => m.EscapedName).Returns(c.ColumnName);
                return columnMetaMock.Object;
            }).ToArray();

            // Create a table metadata provider
            var tableMetaMock = new Mock<IEditTableMetadata>();
            if (allKeys)
            {
                // All columns should be returned as "keys"
                tableMetaMock.Setup(m => m.KeyColumns).Returns(columnMetas);
            }
            else
            {
                // All identity columns should be returned as keys
                tableMetaMock.Setup(m => m.KeyColumns).Returns(columnMetas.Where(c => c.DbColumn.IsIdentity.HasTrue()));
            }
            tableMetaMock.Setup(m => m.Columns).Returns(columnMetas);
            tableMetaMock.Setup(m => m.IsHekaton).Returns(isHekaton);
            tableMetaMock.Setup(m => m.EscapedMultipartName).Returns("tbl");

            return tableMetaMock.Object;
        }

        public static DbColumn[] GetColumns(bool includeIdentity)
        {
            List<DbColumn> columns = new List<DbColumn>();

            if (includeIdentity)
            {
                columns.Add(new TestDbColumn("id", true));
            }

            for (int i = 0; i < 3; i++)
            {
                columns.Add(new TestDbColumn($"col{i}"));
            }
            return columns.ToArray();
        }

        public static ResultSet GetResultSet(DbColumn[] columns, bool includeIdentity)
        {
            object[][] rows = includeIdentity
                ? new[] { new object[] { "id", "1", "2", "3" } }
                : new[] { new object[] { "1", "2", "3" } };
            var testResultSet = new TestResultSet(columns, rows);
            var reader = new TestDbDataReader(new[] { testResultSet });
            var resultSet = new ResultSet(reader, 0, 0, QueryExecution.Common.GetFileStreamFactory(new Dictionary<string, byte[]>()));
            resultSet.ReadResultToEnd(CancellationToken.None).Wait();
            return resultSet;
        }

        public static void AddCells(RowCreate rc, bool includeIdentity)
        {
            // Skip the first column since if identity, since identity columns can't be updated
            int start = includeIdentity ? 1 : 0;
            for (int i = start; i < rc.AssociatedResultSet.Columns.Length; i++)
            {
                rc.SetCell(i, "123");
            }
        }
    }
}
