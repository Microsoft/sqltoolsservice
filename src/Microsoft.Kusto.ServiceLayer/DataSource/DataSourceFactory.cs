﻿using System;
using Microsoft.Kusto.ServiceLayer.Utility;

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    /// <summary>
    /// Data source factory.
    /// </summary>
    public static class DataSourceFactory
    {
        public static IDataSource Create(DataSourceType dataSourceType, string connectionString, string azureAccountToken)
        {
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(connectionString, nameof(connectionString));
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(azureAccountToken, nameof(azureAccountToken));

            switch (dataSourceType)
            {
                case DataSourceType.Kusto:
                {
                    return new KustoDataSource(connectionString, azureAccountToken);
                }

                default:
                    throw new ArgumentException($"Unsupported data source type \"{dataSourceType}\"", nameof(dataSourceType));
            }
        }

        public static DataSourceObjectMetadata CreateClusterMetadata(string clusterName)
        {
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(clusterName, nameof(clusterName));

            return new DataSourceObjectMetadata
            {
                MetadataType = DataSourceMetadataType.Cluster,
                MetadataTypeName = DataSourceMetadataType.Cluster.ToString(),
                Name = clusterName,
                PrettyName = clusterName,
                Urn = $"{clusterName}"
            };
        }

        public static DataSourceObjectMetadata CreateDatabaseMetadata(DataSourceObjectMetadata clusterMetadata, string databaseName)
        {
            ValidationUtils.IsTrue<ArgumentException>(clusterMetadata.MetadataType == DataSourceMetadataType.Cluster, nameof(clusterMetadata));
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(databaseName, nameof(databaseName));

            return new DatabaseMetadata{
                ClusterName = clusterMetadata.Name,
                MetadataType = DataSourceMetadataType.Database,
                MetadataTypeName = DataSourceMetadataType.Database.ToString(),
                Name = databaseName,
                PrettyName = databaseName,
                Urn = $"{clusterMetadata.Urn}.{databaseName}"
            };
        }

        public static DataSourceObjectMetadata CreateFolderMetadata(DataSourceObjectMetadata parentMetadata, string name)
        {
            ValidationUtils.IsNotNull(parentMetadata, nameof(parentMetadata));

            return new FolderMetadata{
                MetadataType = DataSourceMetadataType.Folder,
                MetadataTypeName = DataSourceMetadataType.Folder.ToString(),
                Name = name,
                PrettyName = name,
                ParentMetadata = parentMetadata,
                Urn = $"{parentMetadata.Urn}.Folder_{name}"
            };
        }
    }
}