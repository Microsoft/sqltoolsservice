//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion.Extension;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.UnitTests.ServiceHost;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.LanguageServer
{
    /// <summary>
    /// Tests for the ServiceHost Language Service tests
    /// </summary>
    public class LanguageServiceTests
    {
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

        /// <summary>
        /// Test the service initialization code path and verify nothing throws
        /// </summary>
        [Fact]
        public void ServiceInitialization()
        {
            try
            {
                TestServiceProvider serviceProvider = TestServiceProvider.Instance;
                Assert.NotNull(serviceProvider);
            }
            catch (System.ArgumentException)
            {

            }
            Assert.True(LanguageService.Instance.Context != null);
            Assert.True(LanguageService.Instance.ConnectionServiceInstance != null);
            Assert.True(LanguageService.Instance.CurrentWorkspaceSettings != null);
            Assert.True(LanguageService.Instance.CurrentWorkspace != null);
        }

        /// <summary>
        /// Test the service initialization code path and verify nothing throws
        /// </summary>
        //[Fact]
        public void PrepopulateCommonMetadata()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var result = LiveConnectionHelper.InitLiveConnectionInfo("master", queryTempFile.FilePath);
                var connInfo = result.ConnectionInfo;

                ScriptParseInfo scriptInfo = new ScriptParseInfo { IsConnected = true };

                LanguageService.Instance.PrepopulateCommonMetadata(connInfo, scriptInfo, null);
            }
        }

        // This test currently requires a live database connection to initialize
        // SMO connected metadata provider.  Since we don't want a live DB dependency
        // in the CI unit tests this scenario is currently disabled.
        [Fact]
        public void AutoCompleteFindCompletions()
        {
            var result = GetLiveAutoCompleteTestObjects();

            result.TextDocumentPosition.Position.Character = 7;
            result.ScriptFile.Contents = "select ";

            var autoCompleteService = LanguageService.Instance;
            var completions = autoCompleteService.GetCompletionItems(
                result.TextDocumentPosition,
                result.ScriptFile,
                result.ConnectionInfo).Result;

            Assert.True(completions.Length > 0);
        }

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        /// <summary>
        /// This test currently requires a live database connection to initialize
        /// SMO connected metadata provider.  Since we don't want a live DB dependency
        /// in the CI unit tests this scenario is currently disabled.
        /// </summary>
        [Fact]
        public async void AutoCompleteWithExtension()
        {
            var result = GetLiveAutoCompleteTestObjects();

            result.TextDocumentPosition.Position.Character = 10;
            result.ScriptFile = ScriptFileTests.GetTestScriptFile("select * f");
            result.TextDocumentPosition.TextDocument.Uri = result.ScriptFile.FilePath;

            var autoCompleteService = LanguageService.Instance;
            var requestContext = new Mock<SqlTools.Hosting.Protocol.RequestContext<bool>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<bool>()))
                .Returns(Task.FromResult(new object()));
            var extensionParams = new CompletionExtensionParams()
            {
                Assembly = Path.Combine(AssemblyDirectory, "Microsoft.SqlTools.Test.CompletionExtension.dll"),
                TypeName = "Microsoft.SqlTools.Test.CompletionExtension.CompletionExtProvider1",
                Properties = new Dictionary<string, object> { { "modelPath", "testModel" } }
            };
            await autoCompleteService.HandleCompletionExtLoadRequest(extensionParams, requestContext.Object);
            ScriptParseInfo scriptInfo = new ScriptParseInfo { IsConnected = true };
            autoCompleteService.ParseAndBind(result.ScriptFile, result.ConnectionInfo);
            scriptInfo.ConnectionKey = autoCompleteService.BindingQueue.AddConnectionContext(result.ConnectionInfo);

            var completions = autoCompleteService.GetCompletionItems(
                result.TextDocumentPosition,
                result.ScriptFile,
                result.ConnectionInfo).Result;

            Assert.True(completions.Length > 0 && completions[0].PreSelect.Value);
        }

        /// <summary>
        /// Verify that GetSignatureHelp returns not null when the provided TextDocumentPosition
        /// has an associated ScriptParseInfo and the provided query has a function that should
        /// provide signature help.
        /// </summary>
        [Fact]
        public async Task GetSignatureHelpReturnsNotNullIfParseInfoInitialized()
        {
            // When we make a connection to a live database
            Hosting.ServiceHost.SendEventIgnoreExceptions = true;
            var result = LiveConnectionHelper.InitLiveConnectionInfo();

            // And we place the cursor after a function that should prompt for signature help
            string queryWithFunction = "EXEC sys.fn_isrolemember ";
            result.ScriptFile.Contents = queryWithFunction;
            TextDocumentPosition textDocument = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier
                {
                    Uri = result.ScriptFile.ClientFilePath
                },
                Position = new Position
                {
                    Line = 0,
                    Character = queryWithFunction.Length
                }
            };

            // If the SQL has already been parsed
            var service = LanguageService.Instance;
            await service.UpdateLanguageServiceOnConnection(result.ConnectionInfo);
            Thread.Sleep(2000);

            // We should get back a non-null ScriptParseInfo
            ScriptParseInfo parseInfo = service.GetScriptParseInfo(result.ScriptFile.ClientFilePath);
            Assert.NotNull(parseInfo);

            // And we should get back a non-null SignatureHelp
            SignatureHelp signatureHelp = service.GetSignatureHelp(textDocument, result.ScriptFile);
            Assert.NotNull(signatureHelp);
        }

        /// <summary>
        /// Test overwriting the binding queue context
        /// </summary>
        [Fact]
        public void OverwriteBindingContext()
        {
            var result = LiveConnectionHelper.InitLiveConnectionInfo();

            // add a new connection context
            var connectionKey = LanguageService.Instance.BindingQueue.AddConnectionContext(result.ConnectionInfo, overwrite: true);
            Assert.True(LanguageService.Instance.BindingQueue.BindingContextMap.ContainsKey(connectionKey));

            // cache the server connection
            var orgServerConnection = LanguageService.Instance.BindingQueue.BindingContextMap[connectionKey].ServerConnection;
            Assert.NotNull(orgServerConnection);

            // add a new connection context
            connectionKey = LanguageService.Instance.BindingQueue.AddConnectionContext(result.ConnectionInfo, overwrite: true);
            Assert.True(LanguageService.Instance.BindingQueue.BindingContextMap.ContainsKey(connectionKey));
            Assert.False(object.ReferenceEquals(LanguageService.Instance.BindingQueue.BindingContextMap[connectionKey].ServerConnection, orgServerConnection));
        }

        /// <summary>
        /// Verifies that clearing the Intellisense cache correctly refreshes the cache with new info from the DB.
        /// </summary>
        [Fact]
        public async Task RebuildIntellisenseCacheClearsScriptParseInfoCorrectly()
        {
            var testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, null, null, "LangSvcTest");
            try
            {
                var connectionInfoResult = LiveConnectionHelper.InitLiveConnectionInfo(testDb.DatabaseName);

                var langService = LanguageService.Instance;
                await langService.UpdateLanguageServiceOnConnection(connectionInfoResult.ConnectionInfo);
                var queryText = "SELECT * FROM dbo.";
                connectionInfoResult.ScriptFile.SetFileContents(queryText);

                var textDocumentPosition =
                    connectionInfoResult.TextDocumentPosition ??
                    new TextDocumentPosition()
                    {
                        TextDocument = new TextDocumentIdentifier
                        {
                            Uri = connectionInfoResult.ScriptFile.ClientFilePath
                        },
                        Position = new Position
                        {
                            Line = 0,
                            Character = queryText.Length
                        }
                    };

                // First check that we don't have any items in the completion list as expected
                var initialCompletionItems = langService.GetCompletionItems(
                    textDocumentPosition, connectionInfoResult.ScriptFile, connectionInfoResult.ConnectionInfo);

                Assert.True(initialCompletionItems.Length == 0, $"Should not have any completion items initially. Actual : [{string.Join(',', initialCompletionItems.Select(ci => ci.Label))}]");

                // Now create a table that should show up in the completion list
                testDb.RunQuery("CREATE TABLE dbo.foo(col1 int)");

                // And refresh the cache
                await langService.HandleRebuildIntelliSenseNotification(
                    new RebuildIntelliSenseParams() { OwnerUri = connectionInfoResult.ScriptFile.ClientFilePath },
                    new TestEventContext());

                // Now we should expect to see the item show up in the completion list
                var afterTableCreationCompletionItems = langService.GetCompletionItems(
                    textDocumentPosition, connectionInfoResult.ScriptFile, connectionInfoResult.ConnectionInfo);

                Assert.True(afterTableCreationCompletionItems.Length == 1, $"Should only have a single completion item after rebuilding Intellisense cache. Actual : [{string.Join(',', initialCompletionItems.Select(ci => ci.Label))}]");
                Assert.True(afterTableCreationCompletionItems[0].InsertText == "foo", $"Expected single completion item 'foo'. Actual : [{string.Join(',', initialCompletionItems.Select(ci => ci.Label))}]");
            }
            finally
            {
                testDb.Cleanup();
            }
        }
    }
}
