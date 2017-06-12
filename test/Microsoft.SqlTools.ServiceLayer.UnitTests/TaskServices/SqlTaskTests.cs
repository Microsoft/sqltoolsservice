﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.TaskServices
{
    public class SqlTaskTests
    {
        [Fact]
        public void CreateSqlTaskGivenInvalidArgumentShouldThrowException()
        {
            Assert.Throws<ArgumentNullException>(() => new SqlTask(null, new DatabaseOperationStub().FunctionToRun));
            Assert.Throws<ArgumentNullException>(() => new SqlTask(new TaskMetadata(), null));
        }

        [Fact]
        public void CreateSqlTaskShouldGenerateANewId()
        {
            SqlTask sqlTask = new SqlTask(new TaskMetadata(), new DatabaseOperationStub().FunctionToRun);
            Assert.NotNull(sqlTask.TaskId);
            Assert.True(sqlTask.TaskId != Guid.Empty);

            SqlTask sqlTask2 = new SqlTask(new TaskMetadata(), new DatabaseOperationStub().FunctionToRun);
            Assert.False(sqlTask.TaskId.CompareTo(sqlTask2.TaskId) == 0);
        }

        [Fact]
        public void RunShouldRunTheFunctionAndGetTheResult()
        {
            SqlTaskStatus expectedStatus = SqlTaskStatus.Succeeded;
            DatabaseOperationStub operation = new DatabaseOperationStub();
            operation.TaskResult = new TaskResult
            {
                TaskStatus = expectedStatus
            };
            SqlTask sqlTask = new SqlTask(new TaskMetadata(), operation.FunctionToRun);
            Assert.Equal(sqlTask.TaskStatus, SqlTaskStatus.NotStarted);

            sqlTask.Run().ContinueWith(task => {
                Assert.Equal(sqlTask.TaskStatus, expectedStatus);
                Assert.Equal(sqlTask.IsCompleted, true);
                Assert.True(sqlTask.Duration > 0);
            });
            Assert.Equal(sqlTask.TaskStatus, SqlTaskStatus.InProgress);
            operation.Stop();
        }

        [Fact]
        public void ToTaskInfoShouldReturnTaskInfo()
        {
            SqlTaskStatus expectedStatus = SqlTaskStatus.Succeeded;
            DatabaseOperationStub operation = new DatabaseOperationStub();
            operation.TaskResult = new TaskResult
            {
                TaskStatus = expectedStatus
            };
            SqlTask sqlTask = new SqlTask(new TaskMetadata
            {
                ServerName = "server name",
                DatabaseName = "database name"
            }, operation.FunctionToRun);

            sqlTask.Run().ContinueWith(task =>
            {
                var taskInfo = sqlTask.ToTaskInfo();
                Assert.Equal(taskInfo.TaskId, sqlTask.TaskId.ToString());
                Assert.Equal(taskInfo.ServerName, "server name");
                Assert.Equal(taskInfo.DatabaseName, "database name");
            });
            operation.Stop();
        }

        [Fact]
        public void FailedOperationShouldReturnTheFailedResult()
        {
            SqlTaskStatus expectedStatus = SqlTaskStatus.Failed;
            DatabaseOperationStub operation = new DatabaseOperationStub();
            operation.TaskResult = new TaskResult
            {
                TaskStatus = expectedStatus
            };
            SqlTask sqlTask = new SqlTask(new TaskMetadata(), operation.FunctionToRun);
            Assert.Equal(sqlTask.TaskStatus, SqlTaskStatus.NotStarted);

            sqlTask.Run().ContinueWith(task => {
                Assert.Equal(sqlTask.TaskStatus, expectedStatus);
                Assert.Equal(sqlTask.IsCompleted, true);
                Assert.True(sqlTask.Duration > 0);
            });
            Assert.Equal(sqlTask.TaskStatus, SqlTaskStatus.InProgress);
            operation.Stop();
        }

        [Fact]
        public void CancelingTheTaskShouldCancelTheOperation()
        {
            SqlTaskStatus expectedStatus = SqlTaskStatus.Canceled;
            DatabaseOperationStub operation = new DatabaseOperationStub();
            operation.TaskResult = new TaskResult
            {
            };
            SqlTask sqlTask = new SqlTask(new TaskMetadata(), operation.FunctionToRun);
            Assert.Equal(sqlTask.TaskStatus, SqlTaskStatus.NotStarted);

            sqlTask.Run().ContinueWith(task => {
                Assert.Equal(sqlTask.TaskStatus, expectedStatus);
                Assert.Equal(sqlTask.IsCancelRequested, true);
                Assert.True(sqlTask.Duration > 0);
            });
            Assert.Equal(sqlTask.TaskStatus, SqlTaskStatus.InProgress);
            sqlTask.Cancel();
        }

        [Fact]
        public void FailedOperationShouldFailTheTask()
        {
            SqlTaskStatus expectedStatus = SqlTaskStatus.Failed;
            DatabaseOperationStub operation = new DatabaseOperationStub();
            operation.TaskResult = new TaskResult
            {
            };
            SqlTask sqlTask = new SqlTask(new TaskMetadata(), operation.FunctionToRun);
            Assert.Equal(sqlTask.TaskStatus, SqlTaskStatus.NotStarted);

            sqlTask.Run().ContinueWith(task => {
                Assert.Equal(sqlTask.TaskStatus, expectedStatus);
                Assert.Equal(sqlTask.IsCancelRequested, true);
                Assert.True(sqlTask.Duration > 0);
            });
            Assert.Equal(sqlTask.TaskStatus, SqlTaskStatus.InProgress);
            operation.FailTheOperation();
        }
    }
}
