﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution
{
    public class DisposeTests
    {
        [Fact]
        public void DisposeResultSet()
        {
            // Setup: Mock file stream factory, mock db reader
            var mockFileStreamFactory = new Mock<IFileStreamFactory>();
            var mockDataReader = Common.CreateTestConnection(null, false).CreateCommand().ExecuteReaderAsync().Result;
            
            // If: I setup a single resultset and then dispose it
            ResultSet rs = new ResultSet(mockDataReader, Common.Ordinal, Common.Ordinal, mockFileStreamFactory.Object);
            rs.Dispose();

            // Then: The file that was created should have been deleted
            mockFileStreamFactory.Verify(fsf => fsf.DisposeFile(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task DisposeExecutedQuery()
        {
            // If:
            // ... I request a query (doesn't matter what kind)
            var workspaceService = Common.GetPrimedWorkspaceService(Common.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(null, true, false, workspaceService);
            var executeParams = new ExecuteDocumentSelectionParams {QuerySelection = null, OwnerUri = Common.OwnerUri};
            var executeRequest = RequestContextMocks.Create<ExecuteRequestResult>(null);
            await queryService.HandleExecuteRequest(executeParams, executeRequest.Object);
            await queryService.ActiveQueries[Common.OwnerUri].ExecutionTask;

            // ... And then I dispose of the query
            var disposeParams = new QueryDisposeParams {OwnerUri = Common.OwnerUri};
            var disposeRequest = new EventFlowValidator<QueryDisposeResult>()
                .AddStandardQueryDisposeValidator()
                .Complete();
            await queryService.HandleDisposeRequest(disposeParams, disposeRequest.Object);

            // Then:
            // ... And the active queries should be empty
            disposeRequest.Validate();
            Assert.Empty(queryService.ActiveQueries);
        }

        [Fact]
        public async Task QueryDisposeMissingQuery()
        {
            // If:
            // ... I attempt to dispose a query that doesn't exist
            var workspaceService = new Mock<WorkspaceService<SqlToolsSettings>>();
            var queryService = Common.GetPrimedExecutionService(null, false, false, workspaceService.Object);
            var disposeParams = new QueryDisposeParams {OwnerUri = Common.OwnerUri};

            var disposeRequest = new EventFlowValidator<QueryDisposeResult>()
                .AddErrorValidation<string>(Assert.NotEmpty)
                .Complete();
            await queryService.HandleDisposeRequest(disposeParams, disposeRequest.Object);

            // Then: I should have received an error
            disposeRequest.Validate();
        }

        [Fact]
        public async Task ServiceDispose()
        {
            // Setup:
            // ... We need a query service
            var workspaceService = Common.GetPrimedWorkspaceService(Common.StandardQuery);
            var queryService = Common.GetPrimedExecutionService(null, true, false, workspaceService);

            // If:
            // ... I execute some bogus query
            var queryParams = new ExecuteDocumentSelectionParams { QuerySelection = Common.WholeDocument, OwnerUri = Common.OwnerUri };
            var requestContext = RequestContextMocks.Create<ExecuteRequestResult>(null);
            await queryService.HandleExecuteRequest(queryParams, requestContext.Object);
            await queryService.ActiveQueries[Common.OwnerUri].ExecutionTask;

            // ... And it sticks around as an active query
            Assert.Equal(1, queryService.ActiveQueries.Count);

            // ... The query execution service is disposed, like when the service is shutdown
            queryService.Dispose();

            // Then:
            // ... There should no longer be an active query
            Assert.Empty(queryService.ActiveQueries);
        }
    }

    public static class QueryDisposeEventFlowValidatorExtensions
    {
        public static EventFlowValidator<QueryDisposeResult> AddStandardQueryDisposeValidator(
            this EventFlowValidator<QueryDisposeResult> evf)
        {
            // We just need to make sure that the result is not null
            evf.AddResultValidation(Assert.NotNull);

            return evf;
        }
    }
}
