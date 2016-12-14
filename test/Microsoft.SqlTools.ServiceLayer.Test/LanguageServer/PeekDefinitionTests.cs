//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.Test.Utility;
using Moq;
using Xunit;
using Location = Microsoft.SqlTools.ServiceLayer.Workspace.Contracts.Location;

namespace Microsoft.SqlTools.ServiceLayer.Test.LanguageServices
{
    /// <summary>
    /// Tests for the language service peek definition/ go to definition feature
    /// </summary>
    public class PeekDefinitionTests
    {
        private const int TaskTimeout = 30000;

        private readonly string testScriptUri = TestObjects.ScriptUri;

        private readonly string testConnectionKey = "testdbcontextkey";

        private Mock<ConnectedBindingQueue> bindingQueue;

        private Mock<WorkspaceService<SqlToolsSettings>> workspaceService;

        private Mock<RequestContext<Location[]>> requestContext;

        private Mock<IBinder> binder;

        private TextDocumentPosition textDocument;

        private const string OwnerUri = "testFile1";

        private void InitializeTestObjects()
        {
            // initial cursor position in the script file
            textDocument = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier {Uri = this.testScriptUri},
                Position = new Position
                {
                    Line = 0,
                    Character = 23
                }
            };

            // default settings are stored in the workspace service
            WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings = new SqlToolsSettings();

            // set up file for returning the query
            var fileMock = new Mock<ScriptFile>();
            fileMock.SetupGet(file => file.Contents).Returns(Common.StandardQuery);
            fileMock.SetupGet(file => file.ClientFilePath).Returns(this.testScriptUri);

            // set up workspace mock
            workspaceService = new Mock<WorkspaceService<SqlToolsSettings>>();
            workspaceService.Setup(service => service.Workspace.GetFile(It.IsAny<string>()))
                .Returns(fileMock.Object);

            // setup binding queue mock
            bindingQueue = new Mock<ConnectedBindingQueue>();
            bindingQueue.Setup(q => q.AddConnectionContext(It.IsAny<ConnectionInfo>()))
                .Returns(this.testConnectionKey);

            // inject mock instances into the Language Service
            LanguageService.WorkspaceServiceInstance = workspaceService.Object;
            LanguageService.ConnectionServiceInstance = TestObjects.GetTestConnectionService();
            ConnectionInfo connectionInfo = TestObjects.GetTestConnectionInfo();
            LanguageService.ConnectionServiceInstance.OwnerToConnectionMap.Add(this.testScriptUri, connectionInfo);
            LanguageService.Instance.BindingQueue = bindingQueue.Object;

            // setup the mock for SendResult
            requestContext = new Mock<RequestContext<Location[]>>();
            requestContext.Setup(rc => rc.SendResult(It.IsAny<Location[]>()))
                .Returns(Task.FromResult(0));
            requestContext.Setup(r => r.SendEvent(It.IsAny<EventType<TelemetryParams>>(), It.IsAny<TelemetryParams>()));
            requestContext.Setup(r => r.SendEvent(It.IsAny<EventType<StatusChangeParams>>(), It.IsAny<StatusChangeParams>()));

            // setup the IBinder mock
            binder = new Mock<IBinder>();
            binder.Setup(b => b.Bind(
                It.IsAny<IEnumerable<ParseResult>>(),
                It.IsAny<string>(),
                It.IsAny<BindMode>()));

            var testScriptParseInfo = new ScriptParseInfo();
            LanguageService.Instance.AddOrUpdateScriptParseInfo(this.testScriptUri, testScriptParseInfo);
            testScriptParseInfo.IsConnected = true;
            testScriptParseInfo.ConnectionKey = LanguageService.Instance.BindingQueue.AddConnectionContext(connectionInfo);

            // setup the binding context object
            ConnectedBindingContext bindingContext = new ConnectedBindingContext();
            bindingContext.Binder = binder.Object;
            bindingContext.MetadataDisplayInfoProvider = new MetadataDisplayInfoProvider();
            LanguageService.Instance.BindingQueue.BindingContextMap.Add(testScriptParseInfo.ConnectionKey, bindingContext);
        }


        /// <summary>
        /// Tests the definition event handler. When called with no active connection, no definition is sent
        /// </summary>
        [Fact]
        public async Task DefinitionsHandlerWithNoConnectionTest()
        {
            TestObjects.InitializeTestServices();
            InitializeTestObjects();
            // request the completion list
            await Task.WhenAny(LanguageService.HandleDefinitionRequest(textDocument, requestContext.Object), Task.Delay(TaskTimeout));
            
            // verify that send result was not called
            requestContext.Verify(m => m.SendResult(It.IsAny<Location[]>()), Times.Never());
        }

        /// <summary>
        /// Tests creating location objects on windows and non-windows systems
        /// </summary>
        [Fact]
        public void GetLocationFromFileForValidFilePathTest()
        {
            String filePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "C:\\test\\script.sql" : "/test/script.sql";
            PeekDefinition peekDefinition = new PeekDefinition(null);
            Location[] locations = peekDefinition.GetLocationFromFile(filePath, 0);

            String expectedFilePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "file:///C:/test/script.sql" : "file:/test/script.sql";
            Assert.Equal(locations[0].Uri, expectedFilePath);
        }

        /// <summary>
        /// Test PeekDefinition.GetSchemaFromDatabaseQualifiedName with a valid database name
        /// </summary>
        [Fact]
        public void GetSchemaFromDatabaseQualifiedNameWithValidNameTest()
        {
            PeekDefinition peekDefinition = new PeekDefinition(null);
            string validDatabaseQualifiedName = "master.test.test_table";
            string objectName = "test_table";
            string expectedSchemaName = "test";
        
            string actualSchemaName = peekDefinition.GetSchemaFromDatabaseQualifiedName(validDatabaseQualifiedName, objectName);
            Assert.Equal(actualSchemaName, expectedSchemaName);
        }

        /// <summary>
        /// Test PeekDefinition.GetSchemaFromDatabaseQualifiedName with a valid object name and no schema
        /// </summary>

        [Fact]
        public void GetSchemaFromDatabaseQualifiedNameWithNoSchemaTest()
        {
            PeekDefinition peekDefinition = new PeekDefinition(null);
            string validDatabaseQualifiedName = "test_table";
            string objectName = "test_table";
            string expectedSchemaName = "dbo";
        
            string actualSchemaName = peekDefinition.GetSchemaFromDatabaseQualifiedName(validDatabaseQualifiedName, objectName);
            Assert.Equal(actualSchemaName, expectedSchemaName);
        }

        /// <summary>
        /// Test PeekDefinition.GetSchemaFromDatabaseQualifiedName with a invalid database name
        /// </summary>
        [Fact]
        public void GetSchemaFromDatabaseQualifiedNameWithInvalidNameTest()
        {
            PeekDefinition peekDefinition = new PeekDefinition(null);
            string validDatabaseQualifiedName = "x.y.z";
            string objectName = "test_table";
            string expectedSchemaName = "dbo";
        
            string actualSchemaName = peekDefinition.GetSchemaFromDatabaseQualifiedName(validDatabaseQualifiedName, objectName);
            Assert.Equal(actualSchemaName, expectedSchemaName);
        }
         
#if LIVE_CONNECTION_TESTS
        /// <summary>
        /// Test get definition for a table object with active connection
        /// </summary>
        [Fact]
        public void GetValidTableDefinitionTest()
        {
            // Get live connectionInfo
            PeekDefinition peekDefinition = new PeekDefinition(TestObjects.InitLiveConnectionInfoForDefinition());
            string objectName = "spt_monitor";
            string schemaName = null;
            string objectType = "TABLE";

            // Get locations for valid table object
            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetTableScripts, objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);
        }

        /// <summary>
        /// Test get definition for a invalid table object with active connection
        /// </summary>
        [Fact]
        public void GetTableDefinitionInvalidObjectTest()
        {
            // Get live connectionInfo
            PeekDefinition peekDefinition = new PeekDefinition(TestObjects.InitLiveConnectionInfoForDefinition());
            string objectName = "test_invalid";
            string schemaName = null;
            string objectType = "TABLE";

            // Get locations for invalid table object
            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetTableScripts, objectName, schemaName, objectType);
            Assert.Null(locations);
        }

        /// <summary>
        /// Test get definition for a valid table object with schema and active connection
        /// </summary>
        [Fact]
        public void GetTableDefinitionWithSchemaTest()
        {
            // Get live connectionInfo
            PeekDefinition peekDefinition = new PeekDefinition(TestObjects.InitLiveConnectionInfoForDefinition());
            string objectName = "spt_monitor";
            string schemaName = "dbo";
            string objectType = "TABLE";

            // Get locations for valid table object with schema name
            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetTableScripts, objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);            
        }

        /// <summary>
        /// Test GetDefinition with an unsupported type(function)
        /// </summary>
        [Fact]
        public void GetUnsupportedDefinitionForFullScript()
        {

            ScriptFile scriptFile;
            TextDocumentPosition textDocument = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier { Uri = OwnerUri },
                Position = new Position
                {
                    Line = 0,
                    Character = 20
                }
            };
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfo(out scriptFile);
            scriptFile.Contents = "select * from dbo.func ()";

            var languageService = LanguageService.Instance;
            ScriptParseInfo scriptInfo = new ScriptParseInfo { IsConnected = true };
            languageService.ScriptParseInfoMap.Add(OwnerUri, scriptInfo);

            var locations = languageService.GetDefinition(textDocument, scriptFile, connInfo);
            Assert.Null(locations);
        }

        /// <summary>
        /// Test get definition for a view object with active connection
        /// </summary>
        [Fact]
        public void GetValidViewDefinitionTest()
        {            
            PeekDefinition peekDefinition = new PeekDefinition(TestObjects.InitLiveConnectionInfoForDefinition());
            string objectName = "objects";
            string schemaName = "sys";
            string objectType = "VIEW";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetViewScripts, objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);
        }

        /// <summary>
        /// Test get definition for an invalid view object with no schema name and with active connection
        /// </summary>
        [Fact]
        public void GetViewDefinitionInvalidObjectTest()
        {
            PeekDefinition peekDefinition = new PeekDefinition(TestObjects.InitLiveConnectionInfoForDefinition());
            string objectName = "objects";
            string schemaName = null;
            string objectType = "VIEW";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetViewScripts, objectName, schemaName, objectType);
            Assert.Null(locations);
        }

        /// <summary>
        /// Test get definition for a stored procedure object with active connection
        /// </summary>
        [Fact]
        public void GetStoredProcedureDefinitionTest()
        {
            PeekDefinition peekDefinition = new PeekDefinition(TestObjects.InitLiveConnectionInfoForDefinition());
            string objectName = "sp_MSrepl_startup";
            string schemaName = "dbo";
            string objectType = "PROCEDURE";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetStoredProcedureScripts, objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);
        }

        /// <summary>
        /// Test get definition for a stored procedure object that does not exist with active connection
        /// </summary>
        [Fact]
        public void GetStoredProcedureDefinitionFailureTest()
        {
            PeekDefinition peekDefinition = new PeekDefinition(TestObjects.InitLiveConnectionInfoForDefinition());
            string objectName = "SP2";
            string schemaName = "dbo";
            string objectType = "PROCEDURE";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetStoredProcedureScripts, objectName, schemaName, objectType);
            Assert.Null(locations);
        }

        /// <summary>
        /// Test get definition for a stored procedure object with active connection and no schema
        /// </summary>
        [Fact]
        public void GetStoredProcedureDefinitionWithoutSchemaTest()
        {
            PeekDefinition peekDefinition = new PeekDefinition(TestObjects.InitLiveConnectionInfoForDefinition());
            string objectName = "sp_MSrepl_startup";
            string schemaName = null;
            string objectType = "PROCEDURE";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetStoredProcedureScripts, objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);
        }

        /// <summary>
        /// Helper method to clean up script files
        /// </summary>
        private void Cleanup(Location[] locations)
        {
            Uri fileUri = new Uri(locations[0].Uri);
            if (File.Exists(fileUri.LocalPath))
            {
                try
                {
                    File.Delete(fileUri.LocalPath);
                }
                catch(Exception)
                {

                }
            }
        }
#endif
    }
}
