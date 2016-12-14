﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.Test.Utility;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.Workspace
{
    public class WorkspaceTests
    {
        [Fact]
        public async Task FileClosedSuccessfully()
        {
            // Given:
            // ... A workspace that has a single file open
            var workspace = new ServiceLayer.Workspace.Workspace();
            var workspaceService = new WorkspaceService<SqlToolsSettings> {Workspace = workspace};
            var openedFile = workspace.GetFileBuffer(TestObjects.ScriptUri, string.Empty);
            Assert.NotNull(openedFile);
            Assert.NotEmpty(workspace.GetOpenedFiles());

            // ... And there is a callback registered for the file closed event
            ScriptFile closedFile = null;
            workspaceService.RegisterTextDocCloseCallback((f, c) =>
            {
                closedFile = f;
                return Task.FromResult(true);
            });

            // If:
            // ... An event to close the open file occurs
            var eventContext = new Mock<EventContext>().Object;
            var requestParams = new DidCloseTextDocumentParams
            {
                TextDocument = new TextDocumentItem {Uri = TestObjects.ScriptUri}
            };
            await workspaceService.HandleDidCloseTextDocumentNotification(requestParams, eventContext);

            // Then:
            // ... The file should no longer be in the open files
            Assert.Empty(workspace.GetOpenedFiles());

            // ... The callback should have been called
            // ... The provided script file should be the one we created
            Assert.NotNull(closedFile);
            Assert.Equal(openedFile, closedFile);
        }

        [Fact]
        public async Task FileClosedNotOpen()
        {
            // Given:
            // ... A workspace that has no files open
            var workspace = new ServiceLayer.Workspace.Workspace();
            var workspaceService = new WorkspaceService<SqlToolsSettings> {Workspace = workspace};
            Assert.Empty(workspace.GetOpenedFiles());

            // ... And there is a callback registered for the file closed event
            bool callbackCalled = false;
            workspaceService.RegisterTextDocCloseCallback((f, c) =>
            {
                callbackCalled = true;
                return Task.FromResult(true);
            });

            // If:
            // ... An event to close the a file occurs
            var eventContext = new Mock<EventContext>().Object;
            var requestParams = new DidCloseTextDocumentParams
            {
                TextDocument = new TextDocumentItem {Uri = TestObjects.ScriptUri}
            };
            // Then:
            // ... There should be a file not found exception thrown
            // TODO: This logic should be changed to not create the ScriptFile
            await Assert.ThrowsAnyAsync<IOException>(
                () => workspaceService.HandleDidCloseTextDocumentNotification(requestParams, eventContext));

            // ... There should still be no open files
            // ... The callback should not have been called
            Assert.Empty(workspace.GetOpenedFiles());
            Assert.False(callbackCalled);
        }

        [Fact]
        public void BufferRangeNoneNotNull()
        {
            Assert.NotNull(BufferRange.None); 
        }

        [Fact]
        public void BufferRangeStartGreaterThanEnd()
        {
            Assert.Throws<ArgumentException>(() => 
                new BufferRange(new BufferPosition(2, 2), new BufferPosition(1, 1)));
        }

        [Fact]
        public void BufferRangeEquals()
        {
            var range = new BufferRange(new BufferPosition(1, 1), new BufferPosition(2, 2));
            Assert.False(range.Equals(null));
            Assert.True(range.Equals(range));
            Assert.NotNull(range.GetHashCode());
        }

        [Fact]
        public void UnescapePath()
        {
            Assert.NotNull(Microsoft.SqlTools.ServiceLayer.Workspace.Workspace.UnescapePath("`/path/`"));
        }

        [Fact]
        public void GetBaseFilePath()
        {
            using (var workspace = new ServiceLayer.Workspace.Workspace())
            {
                Assert.Throws<InvalidOperationException>(() => workspace.GetBaseFilePath("path"));
                Assert.NotNull(workspace.GetBaseFilePath(@"c:\path\file.sql"));
                Assert.Equal(workspace.GetBaseFilePath("tsqloutput://c:/path/file.sql"), workspace.WorkspacePath);
            }
        }

        [Fact]
        public void ResolveRelativeScriptPath()
        {
            var workspace = new ServiceLayer.Workspace.Workspace();
            Assert.NotNull(workspace.ResolveRelativeScriptPath(null, @"c:\path\file.sql"));
            Assert.NotNull(workspace.ResolveRelativeScriptPath(@"c:\path\", "file.sql"));
        }
    }
}
