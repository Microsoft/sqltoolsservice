﻿using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Scripts;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Tests;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.PerfTests.Tests
{
    public class ConnectionTests
    {

        [Fact]
        public async Task ConnectAzureTest()
        {
            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            using (TestBase testBase = new TestBase())
            {
                const string query = Scripts.SimpleQuery;
                testBase.WriteToFile(queryFile.FilePath, query);

                DidOpenTextDocumentNotification openParams = new DidOpenTextDocumentNotification
                {
                    TextDocument = new TextDocumentItem
                    {
                        Uri = queryFile.FilePath,
                        LanguageId = "enu",
                        Version = 1,
                        Text = query
                    }
                };

                await testBase.RequestOpenDocumentNotification(openParams);

                Thread.Sleep(500);
                var connected = await Common.CalculateRunTime(async () =>
                {
                    var connectParams = await testBase.GetDatabaseConnectionAsync(TestServerType.Azure);
                    return await testBase.Connect(queryFile.FilePath, connectParams);
                });
                Assert.True(connected, "Connection was not successful");
            }
        }

        [Fact]
        public async Task ConnectOnPremTest()
        {
            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            using (TestBase testBase = new TestBase())
            {
                const string query = Scripts.SimpleQuery;
                testBase.WriteToFile(queryFile.FilePath, query);

                DidOpenTextDocumentNotification openParams = new DidOpenTextDocumentNotification
                {
                    TextDocument = new TextDocumentItem
                    {
                        Uri = queryFile.FilePath,
                        LanguageId = "enu",
                        Version = 1,
                        Text = query
                    }
                };

                await testBase.RequestOpenDocumentNotification(openParams);

                Thread.Sleep(500);
                var connected = await Common.CalculateRunTime(async () =>
                {
                    var connectParams = await testBase.GetDatabaseConnectionAsync(TestServerType.OnPrem);
                    return await testBase.Connect(queryFile.FilePath, connectParams);
                });
                Assert.True(connected, "Connection was not successful");
            }
        }

        [Fact]
        public async Task DisconnectTest()
        {
            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            using (TestBase testBase = new TestBase())
            {
                await Common.ConnectAsync(testBase, TestServerType.OnPrem, Scripts.SimpleQuery, queryFile.FilePath);
                Thread.Sleep(1000);
                var connected = await Common.CalculateRunTime(() => testBase.Disconnect(queryFile.FilePath));
                Assert.True(connected);
            }
        }

    }
}
