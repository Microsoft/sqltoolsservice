﻿using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;
using System.Data.SqlClient;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery
{
    /// <summary>
    /// Interface for backup operations
    /// </summary>
    public interface IBackupUtilities
    {
        /// <summary>
        /// Initialize 
        /// </summary>
        /// <param name="dataContainer"></param>
        /// <param name="sqlConnection"></param>
        void Initialize(CDataContainer dataContainer, SqlConnection sqlConnection);

        /// <summary>
        /// Return database metadata for backup
        /// </summary>
        /// <param name="databaseName"></param>
        /// <returns></returns>
        BackupConfigInfo GetBackupConfigInfo(string databaseName);

        /// <summary>
        /// Set backup input properties
        /// </summary>
        /// <param name="input"></param>
        void SetBackupInput(BackupInfo input);

        /// <summary>
        /// Execute backup
        /// </summary>
        void PerformBackup();
        
        /// <summary>
        /// Cancel backup
        /// </summary>
        void CancelBackup();
    }
}
