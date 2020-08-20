﻿using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.Kusto.ServiceLayer.Utility;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.Kusto.ServiceLayer.DataSource.DataSourceIntellisense;
using Microsoft.Kusto.ServiceLayer.DataSource.Metadata;
using Microsoft.Kusto.ServiceLayer.LanguageServices;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Completion;

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    [Export(typeof(IDataSourceFactory))]
    public class DataSourceFactory : IDataSourceFactory
    {
        private readonly IMetadataFactory _metadataFactory;
        private readonly IKustoIntellisenseHelper _kustoIntellisenseHelper;
        private readonly IAutoCompleteHelper _autoCompleteHelper;

        [ImportingConstructor]
        public DataSourceFactory(IMetadataFactory metadataFactory, IKustoIntellisenseHelper kustoIntellisenseHelper,
            IAutoCompleteHelper autoCompleteHelper)
        {
            _metadataFactory = metadataFactory;
            _kustoIntellisenseHelper = kustoIntellisenseHelper;
            _autoCompleteHelper = autoCompleteHelper;
        }

        public IDataSource Create(DataSourceType dataSourceType, string connectionString, string azureAccountToken)
        {
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(connectionString, nameof(connectionString));
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(azureAccountToken, nameof(azureAccountToken));

            switch (dataSourceType)
            {
                case DataSourceType.Kusto:
                {
                    return new KustoDataSource(connectionString, azureAccountToken, _metadataFactory,
                        _kustoIntellisenseHelper, _autoCompleteHelper);
                }

                default:
                    throw new ArgumentException($"Unsupported data source type \"{dataSourceType}\"",
                        nameof(dataSourceType));
            }
        }

        // Gets default keywords for intellisense when there is no connection.
        public CompletionItem[] GetDefaultAutoComplete(DataSourceType dataSourceType, ScriptDocumentInfo scriptDocumentInfo, Position textDocumentPosition){
            switch (dataSourceType)
            {
                case DataSourceType.Kusto:
                    {
                        return _kustoIntellisenseHelper.GetDefaultKeywords(scriptDocumentInfo, textDocumentPosition);
                    }

                default:
                    throw new ArgumentException($"Unsupported data source type \"{dataSourceType}\"", nameof(dataSourceType));
            }
        }

        // Gets default keywords errors related to intellisense when there is no connection.
        public ScriptFileMarker[] GetDefaultSemanticMarkers(DataSourceType dataSourceType, ScriptParseInfo parseInfo, ScriptFile scriptFile, string queryText){
            switch (dataSourceType)
            {
                case DataSourceType.Kusto:
                    {
                        return _kustoIntellisenseHelper.GetDefaultDiagnostics(parseInfo, scriptFile, queryText);
                    }

                default:
                    throw new ArgumentException($"Unsupported data source type \"{dataSourceType}\"", nameof(dataSourceType));
            }
        }

        public ReliableConnectionHelper.ServerInfo ConvertToServerinfoFormat(DataSourceType dataSourceType, DiagnosticsInfo clusterDiagnostics)
        {
            switch (dataSourceType)
            {
                case DataSourceType.Kusto:
                    {
                        ReliableConnectionHelper.ServerInfo serverInfo = new ReliableConnectionHelper.ServerInfo();
                        serverInfo.Options = new Dictionary<string, object>(clusterDiagnostics.Options);
                        return serverInfo;
                    }

                default:
                    throw new ArgumentException($"Unsupported data source type \"{dataSourceType}\"", nameof(dataSourceType));
            }
        }
    }
}