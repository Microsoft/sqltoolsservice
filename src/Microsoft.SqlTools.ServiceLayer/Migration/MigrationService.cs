//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlServer.DataCollection.Common;
using Microsoft.SqlServer.Management.Assessment.Checks;
using Microsoft.SqlServer.Management.Assessment;
using Microsoft.SqlServer.Migration.Assessment.Common.Contracts.Models;
using Microsoft.SqlServer.Migration.Assessment.Common.Engine;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Migration.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlAssessment;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Migration
{
    /// <summary>
    /// Main class for Migration Service functionality
    /// </summary>
    public sealed class MigrationService : IDisposable
    {
        private static ConnectionService connectionService = null;

        private static readonly Lazy<MigrationService> instance = new Lazy<MigrationService>(() => new MigrationService());

        private bool disposed;

        /// <summary>
        /// Construct a new MigrationService instance with default parameters
        /// </summary>
        public MigrationService()
        {
        }

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static MigrationService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal static ConnectionService ConnectionService
        {
            get
            {
                if (connectionService == null)
                {
                    connectionService = ConnectionService.Instance;
                }
                return connectionService;
            }
            set
            {
                connectionService = value;
            }
        }

        /// <summary>
        /// Gets the <see cref="Engine"/> used to run assessment operations.
        /// </summary>
        internal Engine Engine { get; } = new Engine();

        /// <summary>
        /// Service host object for sending/receiving requests/events.
        /// Internal for testing purposes.
        /// </summary>
        internal IProtocolEndpoint ServiceHost
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes the Migration Service instance
        /// </summary>
        public void InitializeService(ServiceHost serviceHost)
        {
            this.ServiceHost = serviceHost;
            this.ServiceHost.SetRequestHandler(MigrationAssessmentsRequest.Type, HandleMigrationAssessmentsRequest);
        }

        /// <summary>
        /// Handle request to start a migration session
        /// </summary>
        internal async Task HandleMigrationAssessmentsRequest(
            MigrationAssessmentsParams parameters,
            RequestContext<MigrationAssessmentResult> requestContext)
        {
            string randomUri = Guid.NewGuid().ToString();
            try
            {
                // get connection
                if (!ConnectionService.TryFindConnection(parameters.OwnerUri, out var connInfo))
                {
                    await requestContext.SendError("Could not find migration connection");
                    return;
                }

                ConnectParams connectParams = new ConnectParams
                {
                    OwnerUri = randomUri,
                    Connection = connInfo.ConnectionDetails,
                    Type = ConnectionType.Default
                };

                await ConnectionService.Connect(connectParams);

                var connection = await ConnectionService.Instance.GetOrOpenConnection(randomUri, ConnectionType.Default);
                var serverInfo = ReliableConnectionHelper.GetServerVersion(connection);
                var hostInfo = ReliableConnectionHelper.GetServerHostInfo(connection);

                var server = new SqlObjectLocator
                {
                    Connection = connection,
                    EngineEdition = SqlAssessmentService.GetEngineEdition(serverInfo.EngineEditionId),
                    Name = serverInfo.ServerName,
                    ServerName = serverInfo.ServerName,
                    Type = SqlObjectType.Server,
                    Urn = serverInfo.ServerName,
                    Version = Version.Parse(serverInfo.ServerVersion),
                    Platform = hostInfo.Platform
                };
                var connectionStrings = new List<string>();
                if (parameters.Databases != null) 
                {
                    foreach (string database in parameters.Databases)
                    {
                        connInfo.ConnectionDetails.DatabaseName = database;
                        connectionStrings.Add(ConnectionService.BuildConnectionString(connInfo.ConnectionDetails));
                    }
                    string[] assessmentConnectionStrings = connectionStrings.ToArray();
                    var results = await GetAssessmentItems(assessmentConnectionStrings);
                    await requestContext.SendResult(results);
                }
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.ToString());
            }
            finally
            {
                ConnectionService.Disconnect(new DisconnectParams { OwnerUri = randomUri, Type = null });
            }
        }


        internal class AssessmentRequest : IAssessmentRequest
        {
            private readonly Check[] checks = null;

            public AssessmentRequest(ISqlObjectLocator locator)
            {
                Target = locator ?? throw new ArgumentNullException(nameof(locator));
            }

            public EvaluationContext<object> EvaluationContext { get; }

            public ISqlObjectLocator Target { get; }

            public IEnumerable<Check> Checks
            {
                get
                {
                    return checks;
                }
            }

            public bool TryGetData(string column, out object value)
            {
                return EvaluationContext.TryGetData(column, out value);
            }
        }

        internal async Task<MigrationAssessmentResult> GetAssessmentItems(string[] connectionStrings)
        {
            SqlAssessmentConfiguration.EnableLocalLogging = true;
            SqlAssessmentConfiguration.EnableReportCreation = true;
            SqlAssessmentConfiguration.AssessmentReportAndLogsRootFolderPath = Path.GetDirectoryName(Logger.LogFileFullPath);
            DmaEngine engine = new DmaEngine(connectionStrings);
            var assessmentResults = await engine.GetTargetAssessmentResultsList();
            Dictionary<string, ISqlMigrationAssessmentResult> assessmentResultLookup = new Dictionary<string, ISqlMigrationAssessmentResult>();
            foreach (ISqlMigrationAssessmentResult r in assessmentResults)
            {
                assessmentResultLookup.Add(CreateAssessmentResultKey(r as ISqlMigrationAssessmentResult), r as ISqlMigrationAssessmentResult);
            }
            ISqlMigrationAssessmentModel contextualizedAssessmentResult = await engine.GetTargetAssessmentResultsList(System.Threading.CancellationToken.None);
            return new MigrationAssessmentResult()
            {
                AssessmentResult = ParseServerAssessmentInfo(contextualizedAssessmentResult.Servers[0], assessmentResultLookup),
                Errors = ParseAssessmentError(contextualizedAssessmentResult.Errors),
                StartTime = contextualizedAssessmentResult.StartedOn.ToString(),
                EndedTime = contextualizedAssessmentResult.EndedOn.ToString(),
                RawAssessmentResult = contextualizedAssessmentResult
            };
        }

        internal ServerAssessmentProperties ParseServerAssessmentInfo(IServerAssessmentInfo server,  Dictionary<string, ISqlMigrationAssessmentResult> assessmentResultLookup)
        {
            return new ServerAssessmentProperties()
            {
                CpuCoreCount = server.Properties.ServerCoreCount,
                PhysicalServerMemory = server.Properties.MaxServerMemoryInUse,
                ServerHostPlatform = server.Properties.ServerHostPlatform,
                ServerVersion = server.Properties.ServerVersion,
                ServerEngineEdition = server.Properties.ServerEngineEdition,
                ServerEdition = server.Properties.ServerEdition,
                IsClustered = server.Properties.IsClustered,
                NumberOfUserDatabases = server.Properties.NumberOfUserDatabases,
                SqlAssessmentStatus = (int)server.Status,
                AssessedDatabaseCount = server.Properties.NumberOfUserDatabases,
                SQLManagedInstanceTargetReadiness = server.TargetReadinesses[Microsoft.SqlServer.DataCollection.Common.Contracts.Advisor.TargetType.AzureSqlManagedInstance],
                Errors = ParseAssessmentError(server.Errors),
                Items = ParseAssessmentResult(server.ServerAssessments, assessmentResultLookup),
                Databases = ParseDatabaseAssessmentInfo(server.Databases, assessmentResultLookup),
                Name = server.Properties.ServerName
            };
        }

        internal DatabaseAssessmentProperties[] ParseDatabaseAssessmentInfo(IList<IDatabaseAssessmentInfo> databases, Dictionary<string, ISqlMigrationAssessmentResult> assessmentResultLookup)
        {
            return databases.Select(d =>
            {
                return new DatabaseAssessmentProperties()
                {
                    Name = d.Properties.Name,
                    CompatibilityLevel = d.Properties.CompatibilityLevel.ToString(),
                    DatabaseSize = d.Properties.SizeMB,
                    IsReplicationEnabled = d.Properties.IsReplicationEnabled,
                    AssessmentTimeInMilliseconds = d.Properties.TSqlScriptAnalysisTimeElapse.TotalMilliseconds,
                    Errors = ParseAssessmentError(d.Errors),
                    Items = ParseAssessmentResult(d.DatabaseAssessments, assessmentResultLookup),
                    SQLManagedInstanceTargetReadiness = d.TargetReadinesses[Microsoft.SqlServer.DataCollection.Common.Contracts.Advisor.TargetType.AzureSqlManagedInstance]
                };
            }).ToArray();
        }
        internal ErrorModel[] ParseAssessmentError(IList<Microsoft.SqlServer.DataCollection.Common.Contracts.ErrorHandling.IErrorModel> errors)
        {
            return errors.Select(e =>
            {
                return new ErrorModel()
                {
                    ErrorId = e.ErrorID.ToString(),
                    Message = e.Message,
                    ErrorSummary = e.ErrorSummary,
                    PossibleCauses = e.PossibleCauses,
                    Guidance = e.Guidance,
                };
            }).ToArray();
        }
        internal MigrationAssessmentInfo[] ParseAssessmentResult(IList<ISqlMigrationAssessmentResult> assessmentResults, Dictionary<string, ISqlMigrationAssessmentResult> assessmentResultLookup)
        {
            return assessmentResults.Select(r =>
            {
                var check = assessmentResultLookup[CreateAssessmentResultKey(r)].Check;
                return new MigrationAssessmentInfo()
                {
                    CheckId = check.Id,
                    Description = check.Description,
                    DisplayName = check.DisplayName,
                    HelpLink = check.HelpLink,
                    Level = check.Level.ToString(),
                    TargetName = r.AppliesToMigrationTargetPlatform.ToString(),
                    DatabaseName = r.DatabaseName,
                    ServerName = r.ServerName,
                    Tags = check.Tags.ToArray(),
                    RulesetName = Engine.Configuration.DefaultRuleset.Name,
                    RulesetVersion = Engine.Configuration.DefaultRuleset.Version.ToString(),
                    RuleId = r.FeatureId.ToString(),
                    Message = r.Message,
                    AppliesToMigrationTargetPlatform = r.AppliesToMigrationTargetPlatform.ToString(),
                    IssueCategory = r.IssueCategory.ToString(),
                    ImpactedObjects = ParseImpactedObjects(r.ImpactedObjects)
                };
            }).ToArray();
        }
        internal ImpactedObjectInfo[] ParseImpactedObjects(IList<Microsoft.SqlServer.DataCollection.Common.Contracts.Advisor.Models.IImpactedObject> impactedObjects)
        {
            return impactedObjects.Select(i =>
            {
                return new ImpactedObjectInfo()
                {
                    Name = i.Name,
                    ImpactDetail = i.ImpactDetail,
                    ObjectType = i.ObjectType
                };
            }).ToArray();
        }

        internal string CreateAssessmentResultKey(ISqlMigrationAssessmentResult assessment)
        {
            return assessment.ServerName+assessment.DatabaseName+assessment.FeatureId.ToString()+assessment.IssueCategory.ToString()+assessment.Message + assessment.TargetType.ToString() + assessment.AppliesToMigrationTargetPlatform.ToString();
        }

        /// <summary>
        /// Disposes the Migration Service
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
            }
        }
    }
}
