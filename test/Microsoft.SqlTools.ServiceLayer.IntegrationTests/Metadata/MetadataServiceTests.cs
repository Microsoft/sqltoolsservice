//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Xunit;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Metadata;
using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.Metadata.Contracts;
using System.Data.SqlClient;
using System;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.LanguageServer
{
    /// <summary>
    /// Tests for the language service autocomplete component
    /// </summary>
    public class MetadataServiceTests
    {
        private const string TestTableSchema = "dbo";
        private const string TestTableName = "MetadataTestTable";

        private LiveConnectionHelper.TestConnectionResult GetLiveAutoCompleteTestObjects()
        {
            var textDocument = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier { Uri = Constants.OwnerUri },
                Position = new Position
                {
                    Line = 0,
                    Character = 0
                }
            };

            var result = LiveConnectionHelper.InitLiveConnectionInfo();
            result.TextDocumentPosition = textDocument;
            return result;
        }

        private void CreateTestTable(SqlConnection sqlConn)
        {
            string sql = string.Format("IF OBJECT_ID('{0}.{1}', 'U') IS NULL CREATE TABLE {0}.{1}(id int)",
                TestTableSchema, TestTableName);
            using (var sqlCommand = new SqlCommand(sql, sqlConn))
            {
                sqlCommand.ExecuteNonQuery(); 
            }            
        }

        private void DeleteTestTable(SqlConnection sqlConn)
        {
            string sql = string.Format("IF OBJECT_ID('{0}.{1}', 'U') IS NOT NULL DROP TABLE {0}.{1}",
                TestTableSchema, TestTableName);
            using (var sqlCommand = new SqlCommand(sql, sqlConn))
            {
                sqlCommand.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Verify that the metadata service correctly returns details for user tables
        /// </summary>
        [Fact]
        public void MetadataReturnsUserTable()
        {
            var result = GetLiveAutoCompleteTestObjects();
            var sqlConn = MetadataService.OpenMetadataConnection(result.ConnectionInfo);
            Assert.NotNull(sqlConn);

            CreateTestTable(sqlConn);

            var metadata = new List<ObjectMetadata>();
            MetadataService.ReadMetadata(sqlConn, metadata);
            Assert.NotNull(metadata.Count > 0);

            bool foundTestTable = false;
            foreach (var item in metadata)
            {
                if (string.Equals(item.Schema, TestTableSchema, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(item.Name, TestTableName, StringComparison.OrdinalIgnoreCase))
                {
                    foundTestTable = true;
                    break;
                }
            }
            Assert.True(foundTestTable);

            DeleteTestTable(sqlConn);
        }
    }
}
