// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution
{
    /// <summary>
    /// Tests for saving a result set to a file
    /// </summary>
    public class SaveResultsTests
    {
        /// <summary>
        /// Test save results to a file as CSV with correct parameters
        /// </summary>
        [Fact]
        public void SaveResultsAsCsvSuccessTest()
        {
            // Execute a query
            var queryService = Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), true);
            var executeParams = new QueryExecuteParams { QueryText = Common.StandardQuery, OwnerUri = Common.OwnerUri };
            var executeRequest = GetQueryExecuteResultContextMock(null, null, null);
            queryService.HandleExecuteRequest(executeParams, executeRequest.Object).Wait();

            // Request to save the results as csv with correct parameters
            var saveParams = new SaveResultsAsCsvRequestParams { OwnerUri = Common.OwnerUri, ResultSetIndex = 0, BatchIndex = 0 };
            saveParams.FilePath = "testwrite.csv";
            saveParams.IncludeHeaders = true;
            SaveResultRequestResult result = null;
            var saveRequest = GetSaveResultsContextMock(qcr => result = qcr, null);
            queryService.ActiveQueries[Common.OwnerUri].Batches[0] = Common.GetBasicExecutedBatch();
            queryService.HandleSaveResultsAsCsvRequest(saveParams, saveRequest.Object).Wait();

            // Expect to see a file successfully created in filepath and a success message
            Assert.Equal("Success", result.Messages);
            Assert.True(File.Exists(saveParams.FilePath));
            VerifySaveResultsCallCount(saveRequest, Times.Once(), Times.Never());

            // Delete temp file after test
            if(File.Exists(saveParams.FilePath))
            {
                File.Delete(saveParams.FilePath);
            }
        }

        /// <summary>
        /// Test handling exception in saving results to CSV file
        /// </summary>
        [Fact]
        public void SaveResultsAsCsvExceptionTest()
        {
            // Execute a query
            var queryService = Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), true);
            var executeParams = new QueryExecuteParams { QueryText = Common.StandardQuery, OwnerUri = Common.OwnerUri };
            var executeRequest = GetQueryExecuteResultContextMock(null, null, null);
            queryService.HandleExecuteRequest(executeParams, executeRequest.Object).Wait();

            // Request to save the results as csv with incorrect filepath
            var saveParams = new SaveResultsAsCsvRequestParams { OwnerUri = Common.OwnerUri, ResultSetIndex = 0, BatchIndex = 0 };
            if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows))
            {
                saveParams.FilePath = "G:\\test.csv";
            }
            else 
            {
                saveParams.FilePath = "/test.csv";
            }
            // SaveResultRequestResult result = null;
            String errMessage = null;
            var saveRequest = GetSaveResultsContextMock( null, err => errMessage = (String) err);
            queryService.ActiveQueries[Common.OwnerUri].Batches[0] = Common.GetBasicExecutedBatch();
            queryService.HandleSaveResultsAsCsvRequest(saveParams, saveRequest.Object).Wait();

            // Expect to see error message
            Assert.NotNull(errMessage);
            VerifySaveResultsCallCount(saveRequest, Times.Never(), Times.Once());
            Assert.False(File.Exists(saveParams.FilePath));
        }

        /// <summary>
        /// Test saving results to CSV file when the requested result set is no longer active
        /// </summary>
        [Fact]
        public void SaveResultsAsCsvQueryNotFoundTest()
        {
            // Execute a query
            var queryService = Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), true);
            var executeParams = new QueryExecuteParams { QueryText = Common.StandardQuery, OwnerUri = Common.OwnerUri };
            var executeRequest = GetQueryExecuteResultContextMock(null, null, null);
            queryService.HandleExecuteRequest(executeParams, executeRequest.Object).Wait();

            // Request to save the results as csv with query that is no longer active
            var saveParams = new SaveResultsAsCsvRequestParams { OwnerUri = "falseuri", ResultSetIndex = 0, BatchIndex = 0 };
            saveParams.FilePath = "testwrite.csv";
            SaveResultRequestResult result = null;
            var saveRequest = GetSaveResultsContextMock(qcr => result = qcr, null);
            // queryService.ActiveQueries[Common.OwnerUri].Batches[0] = Common.GetBasicExecutedBatch();
            queryService.HandleSaveResultsAsCsvRequest(saveParams, saveRequest.Object).Wait();

            // Expect message that save failed
            Assert.Equal("Failed to save results, ID not found.", result.Messages);
            Assert.False(File.Exists(saveParams.FilePath));
            VerifySaveResultsCallCount(saveRequest, Times.Once(), Times.Never());
        }

        /// <summary>
        /// Test save results to a file as JSON with correct parameters
        /// </summary>
        [Fact]
        public void SaveResultsAsJsonSuccessTest()
        {
            // Execute a query
            var queryService = Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), true);
            var executeParams = new QueryExecuteParams { QueryText = Common.StandardQuery, OwnerUri = Common.OwnerUri };
            var executeRequest = GetQueryExecuteResultContextMock(null, null, null);
            queryService.HandleExecuteRequest(executeParams, executeRequest.Object).Wait();

            // Request to save the results as csv with correct parameters
            var saveParams = new SaveResultsAsJsonRequestParams { OwnerUri = Common.OwnerUri, ResultSetIndex = 0, BatchIndex = 0 };
            saveParams.FilePath = "testwrite.json";
            SaveResultRequestResult result = null;
            var saveRequest = GetSaveResultsContextMock(qcr => result = qcr, null);
            queryService.ActiveQueries[Common.OwnerUri].Batches[0] = Common.GetBasicExecutedBatch();
            queryService.HandleSaveResultsAsJsonRequest(saveParams, saveRequest.Object).Wait();

            // Expect to see a file successfully created in filepath and a success message
            Assert.Equal("Success", result.Messages);
            Assert.True(File.Exists(saveParams.FilePath));
            VerifySaveResultsCallCount(saveRequest, Times.Once(), Times.Never());

            // Delete temp file after test
            if(File.Exists(saveParams.FilePath))
            {
                File.Delete(saveParams.FilePath);
            }
        }

        /// <summary>
        /// Test handling exception in saving results to JSON file
        /// </summary>
        [Fact]
        public void SaveResultsAsJsonExceptionTest()
        {
            // Execute a query
            var queryService = Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), true);
            var executeParams = new QueryExecuteParams { QueryText = Common.StandardQuery, OwnerUri = Common.OwnerUri };
            var executeRequest = GetQueryExecuteResultContextMock(null, null, null);
            queryService.HandleExecuteRequest(executeParams, executeRequest.Object).Wait();

            // Request to save the results as csv with incorrect filepath
            var saveParams = new SaveResultsAsJsonRequestParams { OwnerUri = Common.OwnerUri, ResultSetIndex = 0, BatchIndex = 0 };
            if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows))
            {
                saveParams.FilePath = "G:\\test.json";
            }
            else 
            {
                saveParams.FilePath = "/test.json";
            }
            // SaveResultRequestResult result = null;
            String errMessage = null;
            var saveRequest = GetSaveResultsContextMock( null, err => errMessage = (String) err);
            queryService.ActiveQueries[Common.OwnerUri].Batches[0] = Common.GetBasicExecutedBatch();
            queryService.HandleSaveResultsAsJsonRequest(saveParams, saveRequest.Object).Wait();

            // Expect to see error message
            Assert.NotNull(errMessage);
            VerifySaveResultsCallCount(saveRequest, Times.Never(), Times.Once());
            Assert.False(File.Exists(saveParams.FilePath));
        }

        /// <summary>
        /// Test saving results to JSON file when the requested result set is no longer active
        /// </summary>
        [Fact]
        public void SaveResultsAsJsonQueryNotFoundTest()
        {
            // Execute a query
            var queryService = Common.GetPrimedExecutionService(Common.CreateMockFactory(null, false), true);
            var executeParams = new QueryExecuteParams { QueryText = Common.StandardQuery, OwnerUri = Common.OwnerUri };
            var executeRequest = GetQueryExecuteResultContextMock(null, null, null);
            queryService.HandleExecuteRequest(executeParams, executeRequest.Object).Wait();

            // Request to save the results as csv with query that is no longer active
            var saveParams = new SaveResultsAsJsonRequestParams { OwnerUri = "falseuri", ResultSetIndex = 0, BatchIndex = 0 };
            saveParams.FilePath = "testwrite.json";
            SaveResultRequestResult result = null;
            var saveRequest = GetSaveResultsContextMock(qcr => result = qcr, null);
            // queryService.ActiveQueries[Common.OwnerUri].Batches[0] = Common.GetBasicExecutedBatch();
            queryService.HandleSaveResultsAsJsonRequest(saveParams, saveRequest.Object).Wait();

            // Expect message that save failed
            Assert.Equal("Failed to save results, ID not found.", result.Messages);
            Assert.False(File.Exists(saveParams.FilePath));
            VerifySaveResultsCallCount(saveRequest, Times.Once(), Times.Never());
        }

        #region Mocking

        /// <summary>
        /// Mock the requestContext for saving a result set
        /// </summary>
        /// <param name="resultCallback"></param>
        /// <param name="errorCallback"></param>
        /// <returns></returns>
        private static Mock<RequestContext<SaveResultRequestResult>> GetSaveResultsContextMock(
            Action<SaveResultRequestResult> resultCallback,
            Action<object> errorCallback)
        {
            var requestContext = new Mock<RequestContext<SaveResultRequestResult>>();

            // Setup the mock for SendResult
            var sendResultFlow = requestContext
                .Setup(rc => rc.SendResult(It.IsAny<SaveResultRequestResult> ()))
                .Returns(Task.FromResult(0));
            if (resultCallback != null)
            {
                sendResultFlow.Callback(resultCallback);
            }

            // Setup the mock for SendError
            var sendErrorFlow = requestContext
                .Setup(rc => rc.SendError(It.IsAny<object>()))
                .Returns(Task.FromResult(0));
            if (errorCallback != null)
            {
                sendErrorFlow.Callback(errorCallback);
            }

            return requestContext;
        }

        /// <summary>
        /// Verify the call count for sendResult and error
        /// </summary>
        /// <param name="mock"></param>
        /// <param name="sendResultCalls"></param>
        /// <param name="sendErrorCalls"></param>
        private static void VerifySaveResultsCallCount(Mock<RequestContext<SaveResultRequestResult>> mock,
            Times sendResultCalls, Times sendErrorCalls)
        {
            mock.Verify(rc => rc.SendResult(It.IsAny<SaveResultRequestResult>()), sendResultCalls);
            mock.Verify(rc => rc.SendError(It.IsAny<object>()), sendErrorCalls);
        }

        /// <summary>
        /// Mock request context for executing a  query
        /// </summary>
        /// <param name="resultCallback"></param>
        /// <param name="Action<EventType<QueryExecuteCompleteParams>"></param>
        /// <param name="eventCallback"></param>
        /// <param name="errorCallback"></param>
        /// <returns></returns>
        public static Mock<RequestContext<QueryExecuteResult>> GetQueryExecuteResultContextMock(
            Action<QueryExecuteResult> resultCallback,
            Action<EventType<QueryExecuteCompleteParams>, QueryExecuteCompleteParams> eventCallback,
            Action<object> errorCallback)
        {
            var requestContext = new Mock<RequestContext<QueryExecuteResult>>();

            // Setup the mock for SendResult
            var sendResultFlow = requestContext
                .Setup(rc => rc.SendResult(It.IsAny<QueryExecuteResult>()))
                .Returns(Task.FromResult(0));
            if (resultCallback != null)
            {
                sendResultFlow.Callback(resultCallback);
            }

            // Setup the mock for SendEvent
            var sendEventFlow = requestContext.Setup(rc => rc.SendEvent(
                It.Is<EventType<QueryExecuteCompleteParams>>(m => m == QueryExecuteCompleteEvent.Type),
                It.IsAny<QueryExecuteCompleteParams>()))
                .Returns(Task.FromResult(0));
            if (eventCallback != null)
            {
                sendEventFlow.Callback(eventCallback);
            }

            // Setup the mock for SendError
            var sendErrorFlow = requestContext.Setup(rc => rc.SendError(It.IsAny<object>()))
                .Returns(Task.FromResult(0));
            if (errorCallback != null)
            {
                sendErrorFlow.Callback(errorCallback);
            }

            return requestContext;
        }


        #endregion

    }
}
