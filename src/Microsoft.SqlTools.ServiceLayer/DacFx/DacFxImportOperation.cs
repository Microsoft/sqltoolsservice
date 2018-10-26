﻿using Microsoft.SqlServer.Dac;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.Utility;
using System;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Microsoft.SqlTools.ServiceLayer.DacFx
{
    /// <summary>
    /// Class to represent an in-progress import operation
    /// </summary>
    class DacFxImportOperation : DacFxOperation
    {
        private bool disposed = false;

        public DacFxImportParams Parameters { get; }

        public DacFxImportOperation(DacFxImportParams parameters)
        {
            Validate.IsNotNull("parameters", parameters);

            this.Parameters = parameters;
        }

        public override void Execute()
        {
            if (this.CancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(this.CancellationToken);
            }

            try
            {
                var builder = new SqlConnectionStringBuilder(this.Parameters.ConnectionString);
                DacServices ds = new DacServices(this.Parameters.ConnectionString);
                BacPackage bacpac = BacPackage.Load(this.Parameters.PackageFilePath);
                ds.ImportBacpac(bacpac, this.Parameters.TargetDatabaseName, null);
            }
            catch (Exception e)
            {
                Logger.Write(TraceEventType.Error, string.Format("DacFx import operation {0} failed with exception {1}", this.OperationId, e));
            }
        }

        public override void Dispose()
        {
            if (!disposed)
            {
                this.Cancel();
                disposed = true;
            }
        }
    }
}
