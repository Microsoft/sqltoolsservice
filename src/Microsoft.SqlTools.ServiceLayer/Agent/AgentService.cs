//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Main class for Profiler Service functionality
    /// </summary>
    public sealed class AgentService
    {
        private Dictionary<Guid, JobProperties> jobs = null;
        private ConnectionService connectionService = null;
        private static readonly Lazy<AgentService> instance = new Lazy<AgentService>(() => new AgentService());

        /// <summary>
        /// Construct a new AgentService instance with default parameters
        /// </summary>
        public AgentService()
        {
        }

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static AgentService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal ConnectionService ConnectionServiceInstance
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
        /// Service host object for sending/receiving requests/events.
        /// Internal for testing purposes.
        /// </summary>
        internal IProtocolEndpoint ServiceHost
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        public void InitializeService(ServiceHost serviceHost)
        {
            this.ServiceHost = serviceHost;
            this.ServiceHost.SetRequestHandler(AgentJobsRequest.Type, HandleAgentJobsRequest);
            this.ServiceHost.SetRequestHandler(AgentJobHistoryRequest.Type, HandleJobHistoryRequest);
            this.ServiceHost.SetRequestHandler(AgentJobActionRequest.Type, HandleJobActionRequest);
        }
    
        /// <summary>
        /// Handle request to get Agent job activities
        /// </summary>
        internal async Task HandleAgentJobsRequest(AgentJobsParams parameters, RequestContext<AgentJobsResult> requestContext)
        {
            try
            {
                var result = new AgentJobsResult();
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);

                if (connInfo != null)
                {
                    var sqlConnection = ConnectionService.OpenSqlConnection(connInfo);
                    var serverConnection = new ServerConnection(sqlConnection);
                    var fetcher = new JobFetcher(serverConnection);
                    var filter = new JobActivityFilter();
                    this.jobs = fetcher.FetchJobs(filter);
                    var agentJobs = new List<AgentJobInfo>();
                    if (this.jobs != null)
                    {
                        
                        foreach (var job in this.jobs.Values)
                        {
                            agentJobs.Add(JobUtilities.ConvertToAgentJobInfo(job));
                        }
                    }
                    result.Succeeded = true;
                    result.Jobs = agentJobs.ToArray();
                    sqlConnection.Close();
                }
                await requestContext.SendResult(result);
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        /// <summary>
        /// Handle request to get Agent Job history
        /// </summary>
        internal async Task HandleJobHistoryRequest(AgentJobHistoryParams parameters, RequestContext<AgentJobHistoryResult> requestContext) 
        {
            try 
            {
                var result = new AgentJobHistoryResult();
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);
                if (connInfo != null)
                {
                    Tuple<SqlConnectionInfo, DataTable, ServerConnection> tuple = CreateSqlConnection(connInfo, parameters.JobId);
                    SqlConnectionInfo sqlConnInfo = tuple.Item1;
                    DataTable dt = tuple.Item2;
                    int count = dt.Rows.Count;
                    List<AgentJobHistoryInfo> jobHistories = new List<AgentJobHistoryInfo>();
                    if (count > 0) {
                        var job = dt.Rows[0];
                        string jobName = Convert.ToString(job[JobUtilities.UrnJobName], System.Globalization.CultureInfo.InvariantCulture);
                        Guid jobId = (Guid) job[JobUtilities.UrnJobId];
                        int runStatus = Convert.ToInt32(job[JobUtilities.UrnRunStatus], System.Globalization.CultureInfo.InvariantCulture);
                        var t = new LogSourceJobHistory(jobName, sqlConnInfo, null, runStatus, jobId, null);
                        var tlog = t as ILogSource;
                        tlog.Initialize();
                        var logEntries = t.LogEntries;
                        jobHistories = JobUtilities.ConvertToAgentJobHistoryInfo(logEntries, job);
                        tlog.CloseReader();
                    }
                    result.Jobs = jobHistories.ToArray();
                    result.Succeeded = true;
                    ServerConnection connection = tuple.Item3;
                    connection.Disconnect();
                    await requestContext.SendResult(result);
                }
            }
            catch (Exception e) 
            {
                await requestContext.SendError(e);
            }
        }

        /// <summary>
        /// Handle request to Run a Job
        /// </summary>
        internal async Task HandleJobActionRequest(AgentJobActionParams parameters, RequestContext<AgentJobActionResult> requestContext)
        {
            var result = new AgentJobActionResult();
            try 
            {
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);
                if (connInfo != null)
                {
                    var sqlConnection = ConnectionService.OpenSqlConnection(connInfo);
                    var serverConnection = new ServerConnection(sqlConnection);     
                    var jobHelper = new JobHelper(serverConnection);
                    jobHelper.JobName = parameters.JobName;
                    switch(parameters.Action)
                    {
                        case "run":
                            jobHelper.Start();
                            break;
                        case "stop":
                            jobHelper.Stop();
                            break;
                        case "delete":
                            jobHelper.Delete();
                            break;
                        case "enable":
                            jobHelper.Enable(true);
                            break;
                        case "disable":
                            jobHelper.Enable(false);
                            break;
                        default:
                            break;
                    }
                    result.Succeeded = true;
                    await requestContext.SendResult(result);
                }
            }
            catch (Exception e) 
            {
                result.Succeeded = false;
                result.ErrorMessage = e.Message;
                await requestContext.SendResult(result);
            }
        }

        private Tuple<SqlConnectionInfo, DataTable, ServerConnection> CreateSqlConnection(ConnectionInfo connInfo, String jobId)
        {
            var sqlConnection = ConnectionService.OpenSqlConnection(connInfo);
            var serverConnection = new ServerConnection(sqlConnection);     
            var server = new Server(serverConnection);       
            var filter = new JobHistoryFilter(); 
            filter.JobID = new Guid(jobId);
            var dt = server.JobServer.EnumJobHistory(filter);
            var sqlConnInfo = new SqlConnectionInfo(serverConnection, SqlServer.Management.Common.ConnectionType.SqlConnection);
            return new Tuple<SqlConnectionInfo, DataTable, ServerConnection>(sqlConnInfo, dt, serverConnection);
        }

    }
}
