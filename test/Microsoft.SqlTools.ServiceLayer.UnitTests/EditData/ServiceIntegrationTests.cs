﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.EditData;
using Microsoft.SqlTools.ServiceLayer.EditData.Contracts;
using Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.EditData
{
    public class ServiceIntegrationTests
    {
        #region Session Operation Helper Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" \t\n\r")]
        [InlineData("Does not exist")]
        public async Task NullOrMissingSessionId(string sessionId)
        {
            // Setup: 
            // ... Create a edit data service
            var eds = new EditDataService(null, null, null);

            // ... Create a session params that returns the provided session ID
            var mockParams = new EditCreateRowParams {OwnerUri = sessionId};

            // If: I ask to perform an action that requires a session
            // Then: I should get an error from it
            var efv = new EventFlowValidator<EditDisposeResult>()
                .AddStandardErrorValidation()
                .Complete();
            await eds.HandleSessionRequest(mockParams, efv.Object, session => null);
            efv.Validate();
        }

        [Fact]
        public async Task OperationThrows()
        {
            // Setup: 
            // ... Create an edit data service with a session
            var eds = new EditDataService(null, null, null);
            eds.ActiveSessions[Common.OwnerUri] = GetDefaultSession();

            // ... Create a session param that returns the common owner uri
            var mockParams = new EditCreateRowParams { OwnerUri = Common.OwnerUri };

            // If: I ask to perform an action that requires a session
            // Then: I should get an error from it
            var efv = new EventFlowValidator<EditDisposeResult>()
                .AddStandardErrorValidation()
                .Complete();
            await eds.HandleSessionRequest(mockParams, efv.Object, s => { throw new Exception(); });
            efv.Validate();
        }

        #endregion

        #region Dispose Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" \t\n\r")]
        [InlineData("Does not exist")]
        public async Task DisposeNullOrMissingSessionId(string sessionId)
        {
            // Setup: Create a edit data service
            var eds = new EditDataService(null, null, null);

            // If: I ask to perform an action that requires a session
            // Then: I should get an error from it
            var efv = new EventFlowValidator<EditDisposeResult>()
                .AddStandardErrorValidation()
                .Complete();
            await eds.HandleDisposeRequest(new EditDisposeParams {OwnerUri = sessionId}, efv.Object);
            efv.Validate();
        }

        [Fact]
        public async Task DisposeSuccess()
        {
            // Setup: Create an edit data service with a session
            var eds = new EditDataService(null, null, null);
            eds.ActiveSessions[Common.OwnerUri] = GetDefaultSession();
            
            // If: I ask to dispose of an existing session
            var efv = new EventFlowValidator<EditDisposeResult>()
                .AddResultValidation(Assert.NotNull)
                .Complete();
            await eds.HandleDisposeRequest(new EditDisposeParams {OwnerUri = Common.OwnerUri}, efv.Object);

            // Then:
            // ... It should have completed successfully
            efv.Validate();

            // ... And the session should have been removed from the active session list
            Assert.Empty(eds.ActiveSessions);
        }

        #endregion

        [Fact]
        public async Task DeleteSuccess()
        {
            // Setup: Create an edit data service with a session
            var eds = new EditDataService(null, null, null);
            eds.ActiveSessions[Common.OwnerUri] = GetDefaultSession();

            // If: I validly ask to delete a row
            var efv = new EventFlowValidator<EditDeleteRowResult>()
                .AddResultValidation(Assert.NotNull)
                .Complete();
            await eds.HandleDeleteRowRequest(new EditDeleteRowParams {OwnerUri = Common.OwnerUri, RowId = 0}, efv.Object);

            // Then: 
            // ... It should be successful
            efv.Validate();

            // ... There should be a delete in the session
            Session s = eds.ActiveSessions[Common.OwnerUri];
            Assert.True(s.EditCache.Any(e => e.Value is RowDelete));
        }

        [Fact]
        public async Task CreateSucceeds()
        {
            // Setup: Create an edit data service with a session
            var eds = new EditDataService(null, null, null);
            eds.ActiveSessions[Common.OwnerUri] = GetDefaultSession();

            // If: I ask to create a row from a non existant session
            var efv = new EventFlowValidator<EditCreateRowResult>()
                .AddResultValidation(ecrr => { Assert.True(ecrr.NewRowId > 0); })
                .Complete();
            await eds.HandleCreateRowRequest(new EditCreateRowParams { OwnerUri = Common.OwnerUri }, efv.Object);

            // Then:
            // ... It should have been successful
            efv.Validate();

            // ... There should be a create in the session
            Session s = eds.ActiveSessions[Common.OwnerUri];
            Assert.True(s.EditCache.Any(e => e.Value is RowCreate));
        }

        [Fact]
        public async Task RevertSucceeds()
        {
            // Setup: Create an edit data service with a session that has an pending edit
            var eds = new EditDataService(null, null, null);
            var session = GetDefaultSession();
            session.EditCache[0] = new Mock<RowEditBase>().Object;
            eds.ActiveSessions[Common.OwnerUri] = session;

            // If: I ask to revert a row that has a pending edit
            var efv = new EventFlowValidator<EditRevertRowResult>()
                .AddResultValidation(Assert.NotNull)
                .Complete();
            await eds.HandleRevertRowRequest(new EditRevertRowParams { OwnerUri = Common.OwnerUri, RowId = 0}, efv.Object);

            // Then: 
            // ... It should have succeeded
            efv.Validate();

            // ... The edit cache should be empty again
            Session s = eds.ActiveSessions[Common.OwnerUri];
            Assert.Empty(s.EditCache);
        }

        [Fact]
        public async Task UpdateSuccess()
        {
            // Setup: Create an edit data service with a session
            var eds = new EditDataService(null, null, null);
            var session = GetDefaultSession();
            eds.ActiveSessions[Common.OwnerUri] = session;
            var edit = new Mock<RowEditBase>();
            edit.Setup(e => e.SetCell(It.IsAny<int>(), It.IsAny<string>())).Returns(new EditUpdateCellResult
            {
                NewValue = string.Empty,
                HasCorrections = true,
                IsRevert = false,
                IsNull = false
            });
            session.EditCache[0] = edit.Object;

            // If: I validly ask to update a cell
            var efv = new EventFlowValidator<EditUpdateCellResult>()
                .AddResultValidation(eucr =>
                {
                    Assert.NotNull(eucr);
                    Assert.NotNull(eucr.NewValue);
                    Assert.False(eucr.IsRevert);
                    Assert.False(eucr.IsNull);
                })
                .Complete();
            await eds.HandleUpdateCellRequest(new EditUpdateCellParams { OwnerUri = Common.OwnerUri, RowId = 0}, efv.Object);

            // Then: 
            // ... It should be successful
            efv.Validate();

            // ... Set cell should have been called once
            edit.Verify(e => e.SetCell(It.IsAny<int>(), It.IsAny<string>()), Times.Once);
        }

        private static Session GetDefaultSession()
        {
            // ... Create a session with a proper query and metadata
            Query q = QueryExecution.Common.GetBasicExecutedQuery();
            ResultSet rs = q.Batches[0].ResultSets[0];
            IEditTableMetadata etm = Common.GetMetadata(rs.Columns);
            Session s = new Session(rs, etm);
            return s;
        }
    }

    public static class EditServiceEventFlowValidatorExtensions
    {
        public static EventFlowValidator<T> AddStandardErrorValidation<T>(this EventFlowValidator<T> evf)
        {
            return evf.AddErrorValidation<string>(p =>
            {
                Assert.NotNull(p);
                Assert.NotEmpty(p);
            });
        }
    }
}
