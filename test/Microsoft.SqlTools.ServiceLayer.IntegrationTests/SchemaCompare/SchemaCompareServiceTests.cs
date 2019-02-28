﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.DacFx;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.SchemaCompare
{
    public class SchemaCompareServiceTests
    {
        private const string SourceScript = @"CREATE TABLE [dbo].[table1]
(
    [ID] INT NOT NULL PRIMARY KEY,
    [Date] DATE NOT NULL
)
CREATE TABLE [dbo].[table2]
(
    [ID] INT NOT NULL PRIMARY KEY,
    [col1] NCHAR(10) NULL
)";

        private const string TargetScript = @"CREATE TABLE [dbo].[table2]
(
    [ID] INT NOT NULL PRIMARY KEY,
    [col1] NCHAR(10) NULL,
    [col2] NCHAR(10) NULL
)
CREATE TABLE [dbo].[table3]
(
    [ID] INT NOT NULL PRIMARY KEY,
    [col1] INT NULL,
)";

        private LiveConnectionHelper.TestConnectionResult GetLiveAutoCompleteTestObjects()
        {
            var result = LiveConnectionHelper.InitLiveConnectionInfo();
            return result;
        }

        private async Task<Mock<RequestContext<SchemaCompareResult>>> SendAndValidateSchemaCompareRequestDacpacToDacpac()
        {
            var result = GetLiveAutoCompleteTestObjects();
            var schemaCompareRequestContext = new Mock<RequestContext<SchemaCompareResult>>();
            schemaCompareRequestContext.Setup(x => x.SendResult(It.IsAny<SchemaCompareResult>())).Returns(Task.FromResult(new object()));

            // create dacpacs from databases
            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, SourceScript, "SchemaCompareSource");
            SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, TargetScript, "SchemaCompareTarget");
            string sourceDacpacFilePath = CreateDacpac(sourceDb);
            string targetDacpacFilePath = CreateDacpac(targetDb);

            SchemaCompareEndpointInfo sourceInfo = new SchemaCompareEndpointInfo();
            SchemaCompareEndpointInfo targetInfo = new SchemaCompareEndpointInfo();

            sourceInfo.EndpointType = SchemaCompareEndpointType.dacpac;
            sourceInfo.PackageFilePath = sourceDacpacFilePath;
            targetInfo.EndpointType = SchemaCompareEndpointType.dacpac;
            targetInfo.PackageFilePath = targetDacpacFilePath;

            var schemaCompareParams = new SchemaCompareParams
            {
                sourceEndpointInfo = sourceInfo,
                targetEndpointInfo = targetInfo
            };

            DacFxService service = new DacFxService();
            SchemaCompareOperation schemaCompareOperation = new SchemaCompareOperation(schemaCompareParams, null, null);
            schemaCompareOperation.Execute(TaskExecutionMode.Execute);

            Assert.True(schemaCompareOperation.ComparisonResult.IsValid);
            Assert.False(schemaCompareOperation.ComparisonResult.IsEqual);
            Assert.NotNull(schemaCompareOperation.ComparisonResult.Differences);

            // cleanup
            VerifyAndCleanup(sourceDacpacFilePath);
            VerifyAndCleanup(targetDacpacFilePath);
            sourceDb.Cleanup();
            targetDb.Cleanup();

            return schemaCompareRequestContext;
        }

        private async Task<Mock<RequestContext<SchemaCompareResult>>> SendAndValidateSchemaCompareRequestDatabaseToDatabase()
        {
            var result = GetLiveAutoCompleteTestObjects();
            var schemaCompareRequestContext = new Mock<RequestContext<SchemaCompareResult>>();
            schemaCompareRequestContext.Setup(x => x.SendResult(It.IsAny<SchemaCompareResult>())).Returns(Task.FromResult(new object()));

            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, SourceScript, "SchemaCompareSource");
            SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, TargetScript, "SchemaCompareTarget");
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DacFxTest");
            Directory.CreateDirectory(folderPath);

            SchemaCompareEndpointInfo sourceInfo = new SchemaCompareEndpointInfo();
            SchemaCompareEndpointInfo targetInfo = new SchemaCompareEndpointInfo();

            sourceInfo.EndpointType = SchemaCompareEndpointType.database;
            sourceInfo.DatabaseName = sourceDb.DatabaseName;
            targetInfo.EndpointType = SchemaCompareEndpointType.database;
            targetInfo.DatabaseName = targetDb.DatabaseName;

            var schemaCompareParams = new SchemaCompareParams
            {
                sourceEndpointInfo = sourceInfo,
                targetEndpointInfo = targetInfo
            };

            DacFxService service = new DacFxService();
            SchemaCompareOperation schemaCompareOperation = new SchemaCompareOperation(schemaCompareParams, result.ConnectionInfo, result.ConnectionInfo);
            schemaCompareOperation.Execute(TaskExecutionMode.Execute);

            Assert.True(schemaCompareOperation.ComparisonResult.IsValid);
            Assert.False(schemaCompareOperation.ComparisonResult.IsEqual);
            Assert.NotNull(schemaCompareOperation.ComparisonResult.Differences);

            // cleanup
            sourceDb.Cleanup();
            targetDb.Cleanup();

            return schemaCompareRequestContext;
        }

        private async Task<Mock<RequestContext<SchemaCompareResult>>> SendAndValidateSchemaCompareRequestDatabaseToDacpac()
        {
            var result = GetLiveAutoCompleteTestObjects();
            var schemaCompareRequestContext = new Mock<RequestContext<SchemaCompareResult>>();
            schemaCompareRequestContext.Setup(x => x.SendResult(It.IsAny<SchemaCompareResult>())).Returns(Task.FromResult(new object()));

            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, SourceScript, "SchemaCompareSource");
            SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, TargetScript, "SchemaCompareTarget");
            string targetDacpacFilePath = CreateDacpac(targetDb);

            SchemaCompareEndpointInfo sourceInfo = new SchemaCompareEndpointInfo();
            SchemaCompareEndpointInfo targetInfo = new SchemaCompareEndpointInfo();

            sourceInfo.EndpointType = SchemaCompareEndpointType.database;
            sourceInfo.DatabaseName = sourceDb.DatabaseName;
            targetInfo.EndpointType = SchemaCompareEndpointType.dacpac;
            targetInfo.PackageFilePath = targetDacpacFilePath;

            var schemaCompareParams = new SchemaCompareParams
            {
                sourceEndpointInfo = sourceInfo,
                targetEndpointInfo = targetInfo
            };

            DacFxService service = new DacFxService();
            SchemaCompareOperation schemaCompareOperation = new SchemaCompareOperation(schemaCompareParams, result.ConnectionInfo, null);
            schemaCompareOperation.Execute(TaskExecutionMode.Execute);

            Assert.True(schemaCompareOperation.ComparisonResult.IsValid);
            Assert.False(schemaCompareOperation.ComparisonResult.IsEqual);
            Assert.NotNull(schemaCompareOperation.ComparisonResult.Differences);

            // cleanup
            VerifyAndCleanup(targetDacpacFilePath);
            sourceDb.Cleanup();
            targetDb.Cleanup();

            return schemaCompareRequestContext;
        }

        /// <summary>
        /// Verify the schema compare request comparing two dacpacs
        /// </summary>
        [Fact]
        public void SchemaCompareDacpacToDacpac()
        {
            Assert.NotNull(SendAndValidateSchemaCompareRequestDacpacToDacpac());
        }

        /// <summary>
        /// Verify the schema compare request comparing a two databases
        /// </summary>
        [Fact]
        public async void SchemaCompareDatabaseToDatabase()
        {
            Assert.NotNull(await SendAndValidateSchemaCompareRequestDatabaseToDatabase());
        }

        /// <summary>
        /// Verify the schema compare request comparing a database to a dacpac
        /// </summary>
        [Fact]
        public async void SchemaCompareDatabaseToDacpac()
        {
            Assert.NotNull(await SendAndValidateSchemaCompareRequestDatabaseToDacpac());
        }

        private void VerifyAndCleanup(string filePath)
        {
            // Verify it was created
            Assert.True(File.Exists(filePath));

            // Remove the file
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        private string CreateDacpac(SqlTestDb testdb)
        {
            var result = GetLiveAutoCompleteTestObjects();
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DacFxTest");
            Directory.CreateDirectory(folderPath);

            var extractParams = new ExtractParams
            {
                DatabaseName = testdb.DatabaseName,
                PackageFilePath = Path.Combine(folderPath, string.Format("{0}.dacpac", testdb.DatabaseName)),
                ApplicationName = "test",
                ApplicationVersion = new Version(1, 0)
            };

            DacFxService service = new DacFxService();
            ExtractOperation operation = new ExtractOperation(extractParams, result.ConnectionInfo);
            service.PerformOperation(operation);

            return extractParams.PackageFilePath;
        }
    }
}
