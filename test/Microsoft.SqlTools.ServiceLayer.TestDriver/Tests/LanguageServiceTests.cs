//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.TestDriver.Tests
{
    /// <summary>
    /// Language Service end-to-end integration tests
    /// </summary>
    public class LanguageServiceTests
    {

        /// <summary>
        /// Validate hover tooltip scenarios
        /// </summary>
        [Fact]
        public async Task HoverTest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestHelper testHelper = new TestHelper())
            {
                string query = "SELECT * FROM sys.objects";

                testHelper.WriteToFile(queryTempFile.FilePath, query);

                DidOpenTextDocumentNotification openParams = new DidOpenTextDocumentNotification
                {
                    TextDocument = new TextDocumentItem
                    {
                        Uri = queryTempFile.FilePath,
                        LanguageId = "enu",
                        Version = 1,
                        Text = query
                    }
                };

                await testHelper.RequestOpenDocumentNotification(openParams);
                  
                Thread.Sleep(500);

                bool connected = await testHelper.Connect(queryTempFile.FilePath, ConnectionTestUtils.LocalhostConnection);
                Assert.True(connected, "Connection was not successful");

                Thread.Sleep(10000);

                Hover hover = await testHelper.RequestHover(queryTempFile.FilePath, query, 0, 15);

                Assert.True(hover != null, "Hover tooltop is null");

                await testHelper.Disconnect(queryTempFile.FilePath);
            }
        }

        /// <summary>
        /// Validation autocompletion suggestions scenarios
        /// </summary>
        [Fact]
        public async Task CompletionTest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestHelper testHelper = new TestHelper())
            {
                string query = "SELECT * FROM sys.objects";

                testHelper.WriteToFile(queryTempFile.FilePath, query);

                DidOpenTextDocumentNotification openParams = new DidOpenTextDocumentNotification
                {
                    TextDocument = new TextDocumentItem
                    {
                        Uri = queryTempFile.FilePath,
                        LanguageId = "enu",
                        Version = 1,
                        Text = query
                    }
                };

                await testHelper.RequestOpenDocumentNotification(openParams);
                  
                Thread.Sleep(500);

                bool connected = await testHelper.Connect(queryTempFile.FilePath, ConnectionTestUtils.LocalhostConnection);
                Assert.True(connected, "Connection is successful");

                Thread.Sleep(10000);

                CompletionItem[] completions = await testHelper.RequestCompletion(queryTempFile.FilePath, query, 0, 15);

                Assert.True(completions != null && completions.Length > 0, "Completion items list is null or empty");

                Thread.Sleep(50);

                await testHelper.RequestResolveCompletion(completions[0]);

                Assert.True(completions != null && completions.Length > 0, "Completion items list is null or empty");

                await testHelper.Disconnect(queryTempFile.FilePath);
            }
        }

        /// <summary>
        /// Validate diagnostic scenarios
        /// </summary>
        [Fact]
        public async Task DiagnosticsTests()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestHelper testHelper = new TestHelper())
            {
                bool connected = await testHelper.Connect(queryTempFile.FilePath, ConnectionTestUtils.LocalhostConnection);
                Assert.True(connected, "Connection was not successful");

                Thread.Sleep(500);

                string query = "SELECT *** FROM sys.objects";

                DidOpenTextDocumentNotification openParams = new DidOpenTextDocumentNotification
                {
                    TextDocument = new TextDocumentItem
                    {
                        Uri = queryTempFile.FilePath,
                        LanguageId = "enu",
                        Version = 1,
                        Text = query
                    }
                };

                await testHelper.RequestOpenDocumentNotification(openParams);
              
                Thread.Sleep(100);

                var contentChanges = new TextDocumentChangeEvent[1];
                contentChanges[0] = new TextDocumentChangeEvent
                {
                    Range = new Range
                    {
                        Start = new Position
                        {
                            Line = 0,
                            Character = 5
                        },
                        End = new Position
                        {
                            Line = 0,
                            Character = 6
                        }
                    },
                    RangeLength = 1,
                    Text = "z"
                };

                DidChangeTextDocumentParams changeParams = new DidChangeTextDocumentParams()
                {
                    ContentChanges = contentChanges,
                    TextDocument = new VersionedTextDocumentIdentifier()
                    {
                        Version = 2,
                        Uri = queryTempFile.FilePath
                    }
                };

                await testHelper.RequestChangeTextDocumentNotification(changeParams);

                Thread.Sleep(100);
        
                contentChanges[0] = new TextDocumentChangeEvent
                {
                    Range = new Range
                    {
                        Start = new Position
                        {
                            Line = 0,
                            Character = 5
                        },
                        End = new Position
                        {
                            Line = 0,
                            Character = 6
                        }
                    },
                    RangeLength = 1,
                    Text = "t"
                };

                changeParams = new DidChangeTextDocumentParams
                {
                    ContentChanges = contentChanges,
                    TextDocument = new VersionedTextDocumentIdentifier
                    {
                        Version = 3,
                        Uri = queryTempFile.FilePath
                    }
                };

                await testHelper.RequestChangeTextDocumentNotification(changeParams);

                Thread.Sleep(2500);

                await testHelper.Disconnect(queryTempFile.FilePath);
            }
        }

        /// <summary>
        /// Validate the configuration change event
        /// </summary>
        [Fact]
        public async Task ChangeConfigurationTest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestHelper testHelper = new TestHelper())
            {
                bool connected = await testHelper.Connect(queryTempFile.FilePath, ConnectionTestUtils.LocalhostConnection);
                Assert.True(connected, "Connection was not successful");

                Thread.Sleep(500);             

                var settings = new SqlToolsSettings();
                settings.SqlTools.IntelliSense.EnableIntellisense = false;
                DidChangeConfigurationParams<SqlToolsSettings> configParams = new DidChangeConfigurationParams<SqlToolsSettings>()
                {
                    Settings = settings
                };

                await testHelper.RequestChangeConfigurationNotification(configParams);

                Thread.Sleep(2000);

                await testHelper.Disconnect(queryTempFile.FilePath);
            }
        }

        [Fact]
        public async Task NotificationIsSentAfterOnConnectionAutoCompleteUpdate()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            using (TestHelper testHelper = new TestHelper())
            {
                // Connect
                await testHelper.Connect(queryTempFile.FilePath, ConnectionTestUtils.LocalhostConnection);

                // An event signalling that IntelliSense is ready should be sent shortly thereafter
                var readyParams = await testHelper.Driver.WaitForEvent(IntelliSenseReadyNotification.Type, 30000);
                Assert.NotNull(readyParams);
                Assert.Equal(queryTempFile.FilePath, readyParams.OwnerUri);

                await testHelper.Disconnect(queryTempFile.FilePath);
            }
        }
    }
}
