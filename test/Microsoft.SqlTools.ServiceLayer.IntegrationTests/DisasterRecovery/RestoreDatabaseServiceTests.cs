﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.RestoreOperation;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Xunit;
using static Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility.LiveConnectionHelper;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.DisasterRecovery
{
    public class RestoreDatabaseServiceTests
    {
        private ConnectionService _connectService = TestServiceProvider.Instance.ConnectionService;


        [Fact]
        public async void RestorePlanShouldCreatedSuccessfullyForFullBackup()
        {
            string backupFileName = "FullBackup.bak";
            bool canRestore = true;
            await VerifyRestore(backupFileName, canRestore);
        }

        [Fact]
        public async void RestoreShouldExecuteSuccessfullyForFullBackup()
        {
            string backupFileName = "FullBackup.bak";
            bool canRestore = true;
            var restorePlan = await VerifyRestore(backupFileName, canRestore, true);
        }

        [Fact]
        public async void RestorePlanShouldFailForDiffBackup()
        {
            string backupFileName = "DiffBackup.bak";
            bool canRestore = false;
            await VerifyRestore(backupFileName, canRestore);
        }

        [Fact]
        public async void RestorePlanShouldFailForTransactionLogBackup()
        {
            string backupFileName = "TransactionLogBackup.bak";
            bool canRestore = false;
            await VerifyRestore(backupFileName, canRestore);
        }

        private async Task<RestorePlanResponse> VerifyRestore(string backupFileName, bool canRestore, bool execute = false)
        {
            string filePath = GetBackupFilePath(backupFileName);
            string uri = string.Empty;
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                TestConnectionResult connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                RestoreDatabaseService service = new RestoreDatabaseService();
                var request = new RestoreParams
                {
                    BackupFilePath = filePath,
                    DatabaseName = string.Empty,
                    OwnerUri = queryTempFile.FilePath
                };

                var response = service.CreateRestorePlan(request);

                Assert.NotNull(response);
                Assert.Equal(response.CanRestore, canRestore);
                if (canRestore)
                {
                    Assert.True(response.DbFiles.Any());
                    Assert.Equal(response.DatabaseName, "BackupTestDb");
                    if(execute)
                    {
                        string dropDatabaseQuery = string.Format(CultureInfo.InvariantCulture,
                        Scripts.DropDatabaseIfExist, response.DatabaseName);

                        await TestServiceProvider.Instance.RunQueryAsync(TestServerType.OnPrem, "master", dropDatabaseQuery);
                        request.RelocateDbFiles = response.RelocateFilesNeeded;
                        service.ExecuteRestore(request);
                        Server server = new Server(new ServerConnection(connectionResult.ConnectionInfo.ConnectionDetails.ServerName));
                        Assert.True(server.Databases.Contains(response.DatabaseName));
                    }
                }

                return response;
            }
        }

        private static string TestLocationDirectory
        {
            get
            {
                return Path.Combine(RunEnvironmentInfo.GetTestDataLocation(), "DisasterRecovery");
            }
        }

        public DirectoryInfo BackupFileDirectory
        {
            get
            {
                string d = Path.Combine(TestLocationDirectory, "Backups");
                return new DirectoryInfo(d);
            }
        }

        public FileInfo GetBackupFile(string fileName)
        {
            return new FileInfo(Path.Combine(BackupFileDirectory.FullName, fileName));
        }

        private string GetBackupFilePath(string fileName)
        {
            FileInfo inputFile = GetBackupFile(fileName);
            return inputFile.FullName;
        }
    }
}
