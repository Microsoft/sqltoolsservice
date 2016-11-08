﻿// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution.Execution
{
    public class ResultSetTests
    {
        [Fact]
        public void ResultCreation()
        {
            // If:
            // ... I create a new result set with a valid db data reader

            DbDataReader mockReader = GetReader(null, false, string.Empty);
            ResultSet resultSet = new ResultSet(mockReader, Common.GetFileStreamFactory(new Dictionary<string, byte[]>()));

            // Then:
            // ... There should not be any data read yet
            Assert.Null(resultSet.Columns);
            Assert.Equal(0, resultSet.RowCount);
        }

        [Fact]
        public void ResultCreationInvalidReader()
        {
            // If:
            // ... I create a new result set without a reader
            // Then:
            // ... It should throw an exception
            Assert.Throws<ArgumentNullException>(() => new ResultSet(null, null));

        }

        [Fact]
        public async Task ReadToEndSuccess()
        {
            // If:
            // ... I create a new resultset with a valid db data reader that has data
            // ... and I read it to the end
            DbDataReader mockReader = GetReader(new [] {Common.StandardTestData}, false, Common.StandardQuery);
            var fileStreamFactory = Common.GetFileStreamFactory(new Dictionary<string, byte[]>());
            ResultSet resultSet = new ResultSet(mockReader, fileStreamFactory);
            await resultSet.ReadResultToEnd(CancellationToken.None);

            // Then:
            // ... The columns should be set
            // ... There should be rows to read back
            Assert.NotNull(resultSet.Columns);
            Assert.NotEmpty(resultSet.Columns);
            Assert.Equal(Common.StandardRows, resultSet.RowCount);
        }

        
        [Theory]
        [InlineData("JSON")]
        [InlineData("XML")]
        public async Task ReadToEndForXmlJson(string forType)
        {
            // Setup:
            // ... Build a FOR XML or FOR JSON data set
            string columnName = string.Format("{0}_F52E2B61-18A1-11d1-B105-00805F49916B", forType);
            List<Dictionary<string, string>> data = new List<Dictionary<string, string>>();
            for(int i = 0; i < Common.StandardRows; i++)
            {
                data.Add(new Dictionary<string, string> { { columnName, "test data"} });
            }
            Dictionary<string, string>[][] dataSets = {data.ToArray()};

            // If:
            // ... I create a new resultset with a valid db data reader that is FOR XML/JSON
            // ... and I read it to the end
            DbDataReader mockReader = GetReader(dataSets, false, Common.StandardQuery);
            var fileStreamFactory = Common.GetFileStreamFactory(new Dictionary<string, byte[]>());
            ResultSet resultSet = new ResultSet(mockReader, fileStreamFactory);
            await resultSet.ReadResultToEnd(CancellationToken.None);

            // Then:
            // ... There should only be one column
            // ... There should only be one row
            // ... The result should be marked as complete
            Assert.Equal(1, resultSet.Columns.Length);
            Assert.Equal(1, resultSet.RowCount);

            // If:
            // ... I attempt to read back the results
            // Then: 
            // ... I should only get one row
            var subset = await resultSet.GetSubset(0, 10);
            Assert.Equal(1, subset.RowCount);
        }

        [Fact]
        public async Task GetSubsetWithoutExecution()
        {
            // If:
            // ... I create a new result set with a valid db data reader without executing it
            DbDataReader mockReader = GetReader(null, false, string.Empty);
            var fileStreamFactory = Common.GetFileStreamFactory(new Dictionary<string, byte[]>());
            ResultSet resultSet = new ResultSet(mockReader, fileStreamFactory);

            // Then:
            // ... Attempting to read a subset should fail miserably
            await Assert.ThrowsAsync<InvalidOperationException>(() => resultSet.GetSubset(0, 0));
        }

        [Theory]
        [InlineData(-1, 0)] // Too small start row
        [InlineData(20, 0)] // Too large start row
        [InlineData(0, -1)] // Negative row count
        public async Task GetSubsetInvalidParameters(int startRow, int rowCount)
        {
            // If:
            // ... I create a new result set with a valid db data reader
            // ... And execute the result
            DbDataReader mockReader = GetReader(new[] {Common.StandardTestData}, false, Common.StandardQuery);
            var fileStreamFactory = Common.GetFileStreamFactory(new Dictionary<string, byte[]>());
            ResultSet resultSet = new ResultSet(mockReader, fileStreamFactory);
            await resultSet.ReadResultToEnd(CancellationToken.None);

            // ... And attempt to get a subset with invalid parameters
            // Then:
            // ... It should throw an exception for an invalid parameter
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => resultSet.GetSubset(startRow, rowCount));
        }

        [Theory]
        [InlineData(0, 3)]     // Standard scenario, 3 rows should come back
        [InlineData(0, 20)]    // Asking for too many rows, 5 rows should come back
        [InlineData(1, 3)]     // Standard scenario from non-zero start
        [InlineData(1, 20)]    // Asking for too many rows at a non-zero start
        public async Task GetSubsetSuccess(int startRow, int rowCount)
        {
            // If:
            // ... I create a new result set with a valid db data reader
            // ... And execute the result set
            DbDataReader mockReader = GetReader(new[] { Common.StandardTestData }, false, Common.StandardQuery);
            var fileStreamFactory = Common.GetFileStreamFactory(new Dictionary<string, byte[]>());
            ResultSet resultSet = new ResultSet(mockReader, fileStreamFactory);
            await resultSet.ReadResultToEnd(CancellationToken.None);

            // ... And attempt to get a subset with valid number of rows
            ResultSetSubset subset = await resultSet.GetSubset(startRow, rowCount);

            // Then:
            // ... There should be rows in the subset, either the number of rows or the number of
            //     rows requested or the number of rows in the result set, whichever is lower
            long availableRowsFromStart = resultSet.RowCount - startRow;
            Assert.Equal(Math.Min(availableRowsFromStart, rowCount), subset.RowCount);

            // ... The rows should have the same number of columns as the resultset
            Assert.Equal(resultSet.Columns.Length, subset.Rows[0].Length);
        }

        private static DbDataReader GetReader(Dictionary<string, string>[][] dataSet, bool throwOnRead, string query)
        {
            var info = Common.CreateTestConnectionInfo(dataSet, throwOnRead);
            var connection = info.Factory.CreateSqlConnection(ConnectionService.BuildConnectionString(info.ConnectionDetails));
            var command = connection.CreateCommand();
            command.CommandText = query;
            return command.ExecuteReader();
        }
    }
}
