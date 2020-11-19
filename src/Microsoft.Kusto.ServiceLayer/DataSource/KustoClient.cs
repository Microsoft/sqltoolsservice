using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Kusto.Cloud.Platform.Data;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Data;
using Kusto.Data.Exceptions;
using Kusto.Data.Net.Client;
using Kusto.Language;
using Kusto.Language.Editor;
using Microsoft.Data.SqlClient;
using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.DataSource.DataSourceIntellisense;
using Microsoft.Kusto.ServiceLayer.Utility;

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    public class KustoClient : IKustoClient
    {
        private readonly string _ownerUri;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ICslAdminProvider _kustoAdminProvider;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ICslQueryProvider _kustoQueryProvider;

        /// <summary>
        /// SchemaState used for getting intellisense info.
        /// </summary>
        public GlobalState SchemaState { get; private set; }

        public string ClusterName { get; }
        public string DatabaseName { get; private set; }

        public KustoClient(string connectionString, string azureAccountToken, string ownerUri)
        {
            _ownerUri = ownerUri;
            ClusterName = GetClusterName(connectionString);
            
            var dbName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
            SetDatabaseName(dbName);
            
            Initialize(ClusterName, DatabaseName, azureAccountToken);
            SchemaState = LoadSchemaState();
        }

        private void SetDatabaseName(string databaseName)
        {
            var regex = new Regex(@"(?<=\().+?(?=\))");
            
            DatabaseName = regex.IsMatch(databaseName)
                ? regex.Match(databaseName).Value
                : databaseName;
        }

        private GlobalState LoadSchemaState()
        {
            IEnumerable<ShowDatabaseSchemaResult> tableSchemas = Enumerable.Empty<ShowDatabaseSchemaResult>();
            IEnumerable<ShowFunctionsResult> functionSchemas = Enumerable.Empty<ShowFunctionsResult>();

            if (!string.IsNullOrWhiteSpace(DatabaseName))
            {
                var source = new CancellationTokenSource();
                Parallel.Invoke(() =>
                    {
                        tableSchemas =
                            ExecuteQueryAsync<ShowDatabaseSchemaResult>($".show database {DatabaseName} schema", source.Token, DatabaseName)
                                .Result;
                    },
                    () =>
                    {
                        functionSchemas = ExecuteQueryAsync<ShowFunctionsResult>(".show functions", source.Token, DatabaseName).Result;
                    });
            }

            return KustoIntellisenseHelper.AddOrUpdateDatabase(tableSchemas, functionSchemas,
                GlobalState.Default,
                DatabaseName, ClusterName);
        }

        private void Initialize(string clusterName, string databaseName, string azureAccountToken)
        {
            var stringBuilder = GetKustoConnectionStringBuilder(clusterName, databaseName, azureAccountToken, "", "");
            _kustoQueryProvider = KustoClientFactory.CreateCslQueryProvider(stringBuilder);
            _kustoAdminProvider = KustoClientFactory.CreateCslAdminProvider(stringBuilder);
        }

        private void RefreshAzureToken()
        {
            string azureAccountToken = ConnectionService.Instance.RefreshAzureToken(_ownerUri);
            _kustoQueryProvider.Dispose();
            _kustoAdminProvider.Dispose();
            Initialize(ClusterName, DatabaseName, azureAccountToken);
        }

        /// <summary>
        /// Extracts the cluster name from the connectionstring. The string looks like the following:
        /// "Data Source=clustername.kusto.windows.net;User ID=;Password=;Pooling=False;Application Name=azdata-GeneralConnection"
        /// <summary>
        /// <param name="connectionString">A connection string coming over the Data management protocol</param>
        private string GetClusterName(string connectionString)
        {
            var csb = new SqlConnectionStringBuilder(connectionString);

            // If there is no https:// prefix, add it
            Uri uri;
            if ((Uri.TryCreate(csb.DataSource, UriKind.Absolute, out uri) ||
                 Uri.TryCreate("https://" + csb.DataSource, UriKind.Absolute, out uri)) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return uri.AbsoluteUri;
            }

            throw new ArgumentException("Expected a URL of the form clustername.kusto.windows.net");
        }

        private KustoConnectionStringBuilder GetKustoConnectionStringBuilder(string clusterName, string databaseName,
            string userToken, string applicationClientId, string applicationKey)
        {
            ValidationUtils.IsNotNull(clusterName, nameof(clusterName));
            ValidationUtils.IsTrue<ArgumentException>(
                !string.IsNullOrWhiteSpace(userToken)
                || (!string.IsNullOrWhiteSpace(applicationClientId) && !string.IsNullOrWhiteSpace(applicationKey)),
                $"the Kusto authentication is not specified - either set {nameof(userToken)}, or set {nameof(applicationClientId)} and {nameof(applicationKey)}");

            var kcsb = new KustoConnectionStringBuilder
            {
                DataSource = clusterName,

                // Perform federated auth based on the AAD user token, or based on the AAD application client id and key.
                FederatedSecurity = true
            };

            if (!string.IsNullOrWhiteSpace(databaseName))
            {
                kcsb.InitialCatalog = databaseName;
            }

            if (!string.IsNullOrWhiteSpace(userToken))
            {
                kcsb.UserToken = userToken;
            }

            if (!string.IsNullOrWhiteSpace(applicationClientId))
            {
                kcsb.ApplicationClientId = applicationClientId;
            }

            if (!string.IsNullOrWhiteSpace(applicationKey))
            {
                kcsb.ApplicationKey = applicationKey;
            }

            return kcsb;
        }

        private ClientRequestProperties GetClientRequestProperties(CancellationToken cancellationToken)
        {
            var clientRequestProperties = new ClientRequestProperties
            {
                ClientRequestId = Guid.NewGuid().ToString()
            };
            clientRequestProperties.SetOption(ClientRequestProperties.OptionNoTruncation, true);
            cancellationToken.Register(() => CancelQuery(clientRequestProperties.ClientRequestId));

            return clientRequestProperties;
        }

        public IDataReader ExecuteQuery(string query, CancellationToken cancellationToken, string databaseName = null, int retryCount = 1)
        {
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(query, nameof(query));

            var script = CodeScript.From(query, GlobalState.Default);
            IDataReader[] origReaders = new IDataReader[script.Blocks.Count];
            try
            {
                var numOfQueries = 0;
                Parallel.ForEach(script.Blocks, (codeBlock, state, index) =>
                {
                    var minimalQuery =
                        codeBlock.Service.GetMinimalText(MinimalTextKind.RemoveLeadingWhitespaceAndComments);

                    if (!string.IsNullOrEmpty(minimalQuery))
                    {
                        // Query is empty in case of comments
                        IDataReader origReader;
                        var clientRequestProperties = GetClientRequestProperties(cancellationToken);

                        if (minimalQuery.StartsWith(".") && !minimalQuery.StartsWith(".show"))
                        {
                            origReader = _kustoAdminProvider.ExecuteControlCommand(
                                KustoQueryUtils.IsClusterLevelQuery(minimalQuery) ? "" : databaseName,
                                minimalQuery,
                                clientRequestProperties);
                        }
                        else
                        {
                            origReader = _kustoQueryProvider.ExecuteQuery(
                                KustoQueryUtils.IsClusterLevelQuery(minimalQuery) ? "" : databaseName,
                                minimalQuery,
                                clientRequestProperties);
                        }

                        origReaders[index] = origReader;
                        numOfQueries++;
                    }
                });

                if (numOfQueries == 0 && origReaders.Length > 0) // Covers the scenario when user tries to run comments.
                {
                    var clientRequestProperties = GetClientRequestProperties(cancellationToken);
                    origReaders[0] = _kustoQueryProvider.ExecuteQuery(
                        KustoQueryUtils.IsClusterLevelQuery(query) ? "" : databaseName,
                        query,
                        clientRequestProperties);
                }

                return new KustoResultsReader(origReaders);
            }
            catch (AggregateException exception)
                when (retryCount > 0 &&
                      exception.InnerException is KustoRequestException innerException
                      && innerException.FailureCode == 401) // Unauthorized
            {
                RefreshAzureToken();
                retryCount--;
                return ExecuteQuery(query, cancellationToken, databaseName, retryCount);
            }
        }

        /// <summary>
        /// Executes a query or command against a kusto cluster and returns a sequence of result row instances.
        /// </summary>
        public async Task ExecuteControlCommandAsync(string command, bool throwOnError, int retryCount = 1)
        {
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(command, nameof(command));

            try
            {
                using (var adminOutput = await _kustoAdminProvider.ExecuteControlCommandAsync(DatabaseName, command))
                {
                }
            }
            catch (KustoRequestException exception) when (retryCount > 0 && exception.FailureCode == 401) // Unauthorized
            {
                RefreshAzureToken();
                retryCount--;
                await ExecuteControlCommandAsync(command, throwOnError, retryCount);
            }
            catch (Exception) when (!throwOnError)
            {
            }
        }

        /// <summary>
        /// Executes a query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>The results.</returns>
        public async Task<IEnumerable<T>> ExecuteQueryAsync<T>(string query, CancellationToken cancellationToken, string databaseName = null)
        {
            try
            {
                var resultReader = ExecuteQuery(query, cancellationToken, databaseName);
                var results = KustoDataReaderParser.ParseV1(resultReader, null);
                var tableReader = results[WellKnownDataSet.PrimaryResult].Single().TableData.CreateDataReader();
                return await Task.FromResult(new ObjectReader<T>(tableReader));
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void CancelQuery(string clientRequestId)
        {
            var query = $".cancel query \"{clientRequestId}\"";
            ExecuteControlCommand(query);
        }

        /// <summary>
        /// Executes a Kusto control command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="retryCount"></param>
        public void ExecuteControlCommand(string command, int retryCount = 1)
        {
            ValidationUtils.IsArgumentNotNullOrWhiteSpace(command, nameof(command));

            try
            {
                using (var adminOutput = _kustoAdminProvider.ExecuteControlCommand(command))
                {
                }
            }
            catch (KustoRequestException exception) when (retryCount > 0 && exception.FailureCode == 401) // Unauthorized
            {
                RefreshAzureToken();
                retryCount--;
                ExecuteControlCommand(command, retryCount);
            }
        }

        public void UpdateDatabase(string databaseName)
        {
            SetDatabaseName(databaseName);
            SchemaState = LoadSchemaState();
        }

        public void Dispose()
        {
            _kustoQueryProvider.Dispose();
            _kustoAdminProvider.Dispose();
        }
    }
}