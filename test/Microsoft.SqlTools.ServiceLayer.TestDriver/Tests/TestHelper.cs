//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Credentials.Contracts;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Driver;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.TestDriver.Tests
{
    /// <summary>
    /// Base class for all test suites run by the test driver
    /// </summary>
    public sealed class TestHelper : IDisposable
    {
        private bool isRunning = false;

        public TestHelper()
        {
            Driver = new ServiceTestDriver();
            Driver.Start().Wait();
            this.isRunning = true;
        }

        public void Dispose()
        {
            if (this.isRunning)
            {
                WaitForExit();
            }
        }

        public void WaitForExit()
        {
            try
            {
                this.isRunning = false;
                Driver.Stop().Wait();
                Console.WriteLine("Successfully killed process.");
            }
            catch(Exception e)
            {
                Console.WriteLine($"Exception while waiting for service exit: {e.Message}");
            }
        }

        /// <summary>
        /// The driver object used to read/write data to the service
        /// </summary>
        public ServiceTestDriver Driver
        {
            get;
            private set;
        }

        private object fileLock = new Object();

        /// <summary>
        /// Request a new connection to be created
        /// </summary>
        /// <returns>True if the connection completed successfully</returns>        
        public async Task<bool> Connect(string ownerUri, ConnectParams connectParams, int timeout = 15000)
        { 
            connectParams.OwnerUri = ownerUri;
            var connectResult = await Driver.SendRequest(ConnectionRequest.Type, connectParams);
            if (connectResult)
            {
                var completeEvent = await Driver.WaitForEvent(ConnectionCompleteNotification.Type, timeout);
                return !string.IsNullOrEmpty(completeEvent.ConnectionId);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Request a disconnect
        /// </summary>
        public async Task<bool> Disconnect(string ownerUri)
        {
            var disconnectParams = new DisconnectParams();
            disconnectParams.OwnerUri = ownerUri;

            var disconnectResult = await Driver.SendRequest(DisconnectRequest.Type, disconnectParams);
            return disconnectResult;
        }

        /// <summary>
        /// Request a cancel connect
        /// </summary>
        public async Task<bool> CancelConnect(string ownerUri)
        {
            var cancelParams = new CancelConnectParams();
            cancelParams.OwnerUri = ownerUri;

            return await Driver.SendRequest(CancelConnectRequest.Type, cancelParams);
        }

        /// <summary>
        /// Request a cancel connect
        /// </summary>
        public async Task<ListDatabasesResponse> ListDatabases(string ownerUri)
        {
            var listParams = new ListDatabasesParams();
            listParams.OwnerUri = ownerUri;

            return await Driver.SendRequest(ListDatabasesRequest.Type, listParams);
        }

        /// <summary>
        /// Request the active SQL script is parsed for errors
        /// </summary>
        public async Task<QueryExecuteSubsetResult> RequestQueryExecuteSubset(QueryExecuteSubsetParams subsetParams)
        {
            return await Driver.SendRequest(QueryExecuteSubsetRequest.Type, subsetParams);
        }

        /// <summary>
        /// Request the active SQL script is parsed for errors
        /// </summary>
        public async Task RequestOpenDocumentNotification(DidOpenTextDocumentNotification openParams)
        {
            await Driver.SendEvent(DidOpenTextDocumentNotification.Type, openParams);
        }

        /// <summary>
        /// Request a configuration change notification
        /// </summary>
        public async Task RequestChangeConfigurationNotification(DidChangeConfigurationParams<SqlToolsSettings> configParams)
        {
            await Driver.SendEvent(DidChangeConfigurationNotification<SqlToolsSettings>.Type, configParams);
        }

        /// <summary>
        /// /// Request the active SQL script is parsed for errors
        /// </summary>
        public async Task RequestChangeTextDocumentNotification(DidChangeTextDocumentParams changeParams)
        {
            await Driver.SendEvent(DidChangeTextDocumentNotification.Type, changeParams);
        }
        
        /// <summary>
        /// Request completion item resolve to look-up additional info
        /// </summary>
        public async Task<CompletionItem> RequestResolveCompletion(CompletionItem item)
        {
            var result = await Driver.SendRequest(CompletionResolveRequest.Type, item);
            return result;
        }

        /// <summary>
        /// Request a Read Credential for given credential id
        /// </summary>
        public async Task<Credential> ReadCredential(string credentialId)
        {
            var credentialParams = new Credential();
            credentialParams.CredentialId = credentialId;

            return await Driver.SendRequest(ReadCredentialRequest.Type, credentialParams);
        }

        /// <summary>
        /// Returns database connection parameters for given server type
        /// </summary>
        public async Task<ConnectParams> GetDatabaseConnectionAsync(TestServerType serverType)
        {
            ConnectionProfile connectionProfile = null;
            TestServerIdentity serverIdentiry = ConnectionTestUtils.TestServers.FirstOrDefault(x => x.ServerType == serverType);
            if (serverIdentiry == null)
            {
                connectionProfile = ConnectionTestUtils.Setting.Connections.FirstOrDefault(x => x.ServerType == serverType);
            }
            else
            {
                connectionProfile = ConnectionTestUtils.Setting.GetConnentProfile(serverIdentiry.ProfileName, serverIdentiry.ServerName);
            }

            if (connectionProfile != null)
            {
                string password = connectionProfile.Password;
                if (string.IsNullOrEmpty(password))
                {
                    Credential credential = await ReadCredential(connectionProfile.formatCredentialId());
                    password = credential.Password;
                }
                ConnectParams conenctParam = ConnectionTestUtils.CreateConnectParams(connectionProfile.ServerName, connectionProfile.Database,
                    connectionProfile.User, password);
                return conenctParam;
            }
            return null;
        }

        /// <summary>
        /// Request a list of completion items for a position in a block of text
        /// </summary>
        public async Task<CompletionItem[]> RequestCompletion(string ownerUri, string text, int line, int character)
        {
            // Write the text to a backing file
            lock (fileLock)
            {
                System.IO.File.WriteAllText(ownerUri, text);
            }

            var completionParams = new TextDocumentPosition();
            completionParams.TextDocument = new TextDocumentIdentifier();
            completionParams.TextDocument.Uri = ownerUri;
            completionParams.Position = new Position();
            completionParams.Position.Line = line;
            completionParams.Position.Character = character;

            var result = await Driver.SendRequest(CompletionRequest.Type, completionParams);
            return result;
        }

        /// <summary>
        /// Request a a hover tooltop
        /// </summary>
        public async Task<Hover> RequestHover(string ownerUri, string text, int line, int character)
        {
            // Write the text to a backing file
            lock (fileLock)
            {
                System.IO.File.WriteAllText(ownerUri, text);
            }

            var completionParams = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier {Uri = ownerUri},
                Position = new Position
                {
                    Line = line,
                    Character = character
                }
            };

            var result = await Driver.SendRequest(HoverRequest.Type, completionParams);
            return result;
        }

        /// <summary>
        /// Run a query using a given connection bound to a URI
        /// </summary>
        public async Task<QueryExecuteCompleteParams> RunQuery(string ownerUri, string query, int timeoutMilliseconds = 5000)
        {
            // Write the query text to a backing file
            WriteToFile(ownerUri, query);

            var queryParams = new QueryExecuteParams
            {
                OwnerUri = ownerUri,
                QuerySelection = null
            };

            var result = await Driver.SendRequest(QueryExecuteRequest.Type, queryParams);
            if (result != null && string.IsNullOrEmpty(result.Messages))
            {
                var eventResult = await Driver.WaitForEvent(QueryExecuteCompleteEvent.Type, timeoutMilliseconds);
                return eventResult;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Run a query using a given connection bound to a URI. This method only waits for the initial response from query
        /// execution (QueryExecuteResult). It is up to the caller to wait for the QueryExecuteCompleteEvent if they are interested.
        /// </summary>
        public async Task<QueryExecuteResult> RunQueryAsync(string ownerUri, string query, int timeoutMilliseconds = 5000)
        {
            WriteToFile(ownerUri, query);

            var queryParams = new QueryExecuteParams
            {
                OwnerUri = ownerUri,
                QuerySelection = null
            };

            return await Driver.SendRequest(QueryExecuteRequest.Type, queryParams);
        }
        
        /// <summary>
        /// Request to cancel an executing query
        /// </summary>
        public async Task<QueryCancelResult> CancelQuery(string ownerUri)
        {
            var cancelParams = new QueryCancelParams {OwnerUri = ownerUri};

            var result = await Driver.SendRequest(QueryCancelRequest.Type, cancelParams);
            return result;
        }

        /// <summary>
        /// Request to save query results as CSV
        /// </summary>
        public async Task<SaveResultRequestResult> SaveAsCsv(string ownerUri, string filename, int batchIndex, int resultSetIndex)
        {
            var saveParams = new SaveResultsAsCsvRequestParams
            {
                OwnerUri = ownerUri,
                BatchIndex = batchIndex,
                ResultSetIndex = resultSetIndex,
                FilePath = filename
            };

            var result = await Driver.SendRequest(SaveResultsAsCsvRequest.Type, saveParams);
            return result;
        }

        /// <summary>
        /// Request to save query results as JSON
        /// </summary>
        public async Task<SaveResultRequestResult> SaveAsJson(string ownerUri, string filename, int batchIndex, int resultSetIndex)
        {
            var saveParams = new SaveResultsAsJsonRequestParams
            {
                OwnerUri = ownerUri,
                BatchIndex = batchIndex,
                ResultSetIndex = resultSetIndex,
                FilePath = filename
            };

            var result = await Driver.SendRequest(SaveResultsAsJsonRequest.Type, saveParams);
            return result;
        }

        /// <summary>
        /// Request a subset of results from a query
        /// </summary>
        public async Task<QueryExecuteSubsetResult> ExecuteSubset(string ownerUri, int batchIndex, int resultSetIndex, int rowStartIndex, int rowCount)
        {
            var subsetParams = new QueryExecuteSubsetParams();
            subsetParams.OwnerUri = ownerUri;
            subsetParams.BatchIndex = batchIndex;
            subsetParams.ResultSetIndex = resultSetIndex;
            subsetParams.RowsStartIndex = rowStartIndex;
            subsetParams.RowsCount = rowCount;

            var result = await Driver.SendRequest(QueryExecuteSubsetRequest.Type, subsetParams);
            return result;
        }

        public void WriteToFile(string ownerUri, string query)
        {
            lock (fileLock)
            {
                System.IO.File.WriteAllText(ownerUri, query);
            }
        }
    }
}
