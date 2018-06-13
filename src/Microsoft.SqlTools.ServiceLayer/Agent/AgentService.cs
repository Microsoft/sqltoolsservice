//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Main class for Agent Service functionality
    /// </summary>
    public class AgentService
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

            // Jobs request handlers
            this.ServiceHost.SetRequestHandler(AgentJobsRequest.Type, HandleAgentJobsRequest);
            this.ServiceHost.SetRequestHandler(AgentJobHistoryRequest.Type, HandleJobHistoryRequest);
            this.ServiceHost.SetRequestHandler(AgentJobActionRequest.Type, HandleJobActionRequest);

            this.ServiceHost.SetRequestHandler(CreateAgentJobRequest.Type, HandleCreateAgentJobRequest);
            this.ServiceHost.SetRequestHandler(UpdateAgentJobRequest.Type, HandleUpdateAgentJobRequest);
            this.ServiceHost.SetRequestHandler(DeleteAgentJobRequest.Type, HandleDeleteAgentJobRequest);

            // Job Steps request handlers
            this.ServiceHost.SetRequestHandler(CreateAgentJobStepRequest.Type, HandleCreateAgentJobStepRequest);
            this.ServiceHost.SetRequestHandler(UpdateAgentJobStepRequest.Type, HandleUpdateAgentJobStepRequest);
            this.ServiceHost.SetRequestHandler(DeleteAgentJobStepRequest.Type, HandleDeleteAgentJobStepRequest);

            // Alerts request handlers
            this.ServiceHost.SetRequestHandler(AgentAlertsRequest.Type, HandleAgentAlertsRequest);
            this.ServiceHost.SetRequestHandler(CreateAgentAlertRequest.Type, HandleCreateAgentAlertRequest);
            this.ServiceHost.SetRequestHandler(UpdateAgentAlertRequest.Type, HandleUpdateAgentAlertRequest);
            this.ServiceHost.SetRequestHandler(DeleteAgentAlertRequest.Type, HandleDeleteAgentAlertRequest);

            // Operators request handlers
            this.ServiceHost.SetRequestHandler(AgentOperatorsRequest.Type, HandleAgentOperatorsRequest);
            this.ServiceHost.SetRequestHandler(CreateAgentOperatorRequest.Type, HandleCreateAgentOperatorRequest);
            this.ServiceHost.SetRequestHandler(UpdateAgentOperatorRequest.Type, HandleUpdateAgentOperatorRequest);
            this.ServiceHost.SetRequestHandler(DeleteAgentOperatorRequest.Type, HandleDeleteAgentOperatorRequest);

            // Proxy Accounts request handlers
            this.ServiceHost.SetRequestHandler(AgentProxiesRequest.Type, HandleAgentProxiesRequest);
            this.ServiceHost.SetRequestHandler(CreateAgentProxyRequest.Type, HandleCreateAgentProxyRequest);
            this.ServiceHost.SetRequestHandler(UpdateAgentProxyRequest.Type, HandleUpdateAgentProxyRequest);
            this.ServiceHost.SetRequestHandler(DeleteAgentProxyRequest.Type, HandleDeleteAgentProxyRequest);
        }

        #region "Jobs Handlers"

        /// <summary>
        /// Handle request to get Agent job activities
        /// </summary>
        internal async Task HandleAgentJobsRequest(AgentJobsParams parameters, RequestContext<AgentJobsResult> requestContext)
        {
            await Task.Run(async () =>
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
                        result.Success = true;
                        result.Jobs = agentJobs.ToArray();
                        sqlConnection.Close();
                    }
                    await requestContext.SendResult(result);
                }
                catch (Exception e)
                {
                    await requestContext.SendError(e);
                }
            });
        }

        /// <summary>
        /// Handle request to get Agent Job history
        /// </summary>
        internal async Task HandleJobHistoryRequest(AgentJobHistoryParams parameters, RequestContext<AgentJobHistoryResult> requestContext)
        {
            await Task.Run(async () =>
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
                        ServerConnection connection = tuple.Item3;
                        int count = dt.Rows.Count;
                        List<AgentJobHistoryInfo> jobHistories = new List<AgentJobHistoryInfo>();
                        if (count > 0)
                        {
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
                        result.Success = true;
                        connection.Disconnect();
                        await requestContext.SendResult(result);
                    }
                }
                catch (Exception e)
                {
                    await requestContext.SendError(e);
                }
            });
        }

        /// <summary>
        /// Handle request to Run a Job
        /// </summary>
        internal async Task HandleJobActionRequest(AgentJobActionParams parameters, RequestContext<ResultStatus> requestContext)
        {
            await Task.Run(async () =>
            {
                var result = new ResultStatus();
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
                        result.Success = true;
                        await requestContext.SendResult(result);
                    }
                }
                catch (Exception e)
                {
                    result.Success = false;
                    result.ErrorMessage = e.Message;
                    await requestContext.SendResult(result);
                }
            });
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

        internal async Task HandleCreateAgentJobRequest(CreateAgentJobParams parameters, RequestContext<CreateAgentJobResult> requestContext)
        {
            var result = await ConfigureAgentJob(
                parameters.OwnerUri,
                parameters.Job,
                ConfigAction.Create,
                ManagementUtils.asRunType(parameters.TaskExecutionMode));

            await requestContext.SendResult(new CreateAgentJobResult()
            {
                Success = result.Item1,
                ErrorMessage = result.Item2
            });
        }

        internal async Task HandleUpdateAgentJobRequest(UpdateAgentJobParams parameters, RequestContext<UpdateAgentJobResult> requestContext)
        {
            var result = await ConfigureAgentJob(
                parameters.OwnerUri,
                parameters.Job,
                ConfigAction.Update,
                ManagementUtils.asRunType(parameters.TaskExecutionMode));

            await requestContext.SendResult(new UpdateAgentJobResult()
            {
                Success = result.Item1,
                ErrorMessage = result.Item2
            });
        }

        internal async Task HandleDeleteAgentJobRequest(DeleteAgentJobParams parameters, RequestContext<ResultStatus> requestContext)
        {
             var result = await ConfigureAgentJob(
                parameters.OwnerUri,
                parameters.Job,
                ConfigAction.Drop,
                ManagementUtils.asRunType(parameters.TaskExecutionMode));

            await requestContext.SendResult(new ResultStatus()
            {
                Success = result.Item1,
                ErrorMessage = result.Item2
            });
        }

        internal async Task HandleCreateAgentJobStepRequest(CreateAgentJobStepParams parameters, RequestContext<CreateAgentJobStepResult> requestContext)
        {
            Tuple<bool, string> result = await ConfigureAgentJobStep(
                parameters.OwnerUri,
                parameters.Step,
                ConfigAction.Create,
                RunType.RunNow);

            await requestContext.SendResult(new CreateAgentJobStepResult()
            {
                Success = result.Item1,
                ErrorMessage = result.Item2
            });
        }

        internal async Task HandleUpdateAgentJobStepRequest(UpdateAgentJobStepParams parameters, RequestContext<UpdateAgentJobStepResult> requestContext)
        {
            Tuple<bool, string> result = await ConfigureAgentJobStep(
                parameters.OwnerUri,
                parameters.Step,
                ConfigAction.Update,
                RunType.RunNow);

            await requestContext.SendResult(new UpdateAgentJobStepResult()
            {
                Success = result.Item1,
                ErrorMessage = result.Item2
            });
        }

        internal async Task HandleDeleteAgentJobStepRequest(DeleteAgentJobStepParams parameters, RequestContext<ResultStatus> requestContext)
        {
            Tuple<bool, string> result = await ConfigureAgentJobStep(
                parameters.OwnerUri,
                parameters.Step,
                ConfigAction.Drop,
                RunType.RunNow);

            await requestContext.SendResult(new ResultStatus()
            {
                Success = result.Item1,
                ErrorMessage = result.Item2
            });
        }

        private void CreateJobData(
            string ownerUri, 
            string jobName,
            out CDataContainer dataContainer,
            out JobData jobData,
            AgentJobInfo jobInfo = null)
        {
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(
                ownerUri,
                out connInfo);

            dataContainer = CDataContainer.CreateDataContainer(
                connInfo, 
                databaseExists: true);

            XmlDocument jobDoc = CreateJobXmlDocument(
                dataContainer.Server.Name.ToUpper(),
                jobName);

            dataContainer.Init(jobDoc.InnerXml);

            STParameters param = new STParameters(dataContainer.Document);
            param.SetParam("job", string.Empty);                    
            param.SetParam("jobid", string.Empty);

            jobData = new JobData(dataContainer, jobInfo);
        }

        internal async Task<Tuple<bool, string>> ConfigureAgentJob(
            string ownerUri,
            AgentJobInfo jobInfo,
            ConfigAction configAction,
            RunType runType)
        {
            return await Task<Tuple<bool, string>>.Run(() =>
            {
                try
                {
                    JobData jobData;
                    CDataContainer dataContainer;
                    CreateJobData(ownerUri, jobInfo.Name, out dataContainer, out jobData, jobInfo);

                    using (JobActions jobActions = new JobActions(dataContainer, jobData, configAction))
                    {
                        var executionHandler = new ExecutonHandler(jobActions);
                        executionHandler.RunNow(runType, this);
                    }

                    return new Tuple<bool, string>(true, string.Empty);
                }
                catch (Exception ex)
                {
                    return new Tuple<bool, string>(false, ex.ToString());
                }
            });
        }

        internal async Task<Tuple<bool, string>> ConfigureAgentJobStep(
            string ownerUri,
            AgentJobStepInfo stepInfo,
            ConfigAction configAction,
            RunType runType)
        {
            return await Task<Tuple<bool, string>>.Run(() =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(stepInfo.JobName))
                    {
                        return new Tuple<bool, string>(false, "JobId cannot be null");
                    }

                    JobData jobData;
                    CDataContainer dataContainer;
                    CreateJobData(ownerUri, stepInfo.JobName, out dataContainer, out jobData);

                    using (var jobStep = new JobStepsActions(dataContainer, jobData, stepInfo, configAction))
                    {
                        var executionHandler = new ExecutonHandler(jobStep);
                        executionHandler.RunNow(runType, this);
                    }

                    return new Tuple<bool, string>(true, string.Empty);
                }
                catch (Exception ex)
                {
                    // log exception here
                    return new Tuple<bool, string>(false, ex.ToString());
                }
            });
        }

        public static XmlDocument CreateJobXmlDocument(string svrName, string jobName)
        {
            // XML element strings
            const string XmlFormDescElementName = "formdescription";
            const string XmlParamsElementName = "params";
            const string XmlJobElementName = "job";
            const string XmlUrnElementName = "urn";
            const string UrnFormatStr = "Server[@Name='{0}']/JobServer[@Name='{0}']/Job[@Name='{1}']";

            // Write out XML.
            StringWriter textWriter = new StringWriter();
            XmlTextWriter xmlWriter = new XmlTextWriter(textWriter);

            xmlWriter.WriteStartElement(XmlFormDescElementName);
            xmlWriter.WriteStartElement(XmlParamsElementName);

            xmlWriter.WriteElementString(XmlJobElementName, jobName);
            xmlWriter.WriteElementString(XmlUrnElementName, string.Format(UrnFormatStr, svrName, jobName));
    
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndElement();
    
            xmlWriter.Close();

            // Create an XML document.
            XmlDocument doc = new XmlDocument();
            XmlTextReader rdr = new XmlTextReader(new System.IO.StringReader(textWriter.ToString()));
            rdr.MoveToContent();
            doc.LoadXml(rdr.ReadOuterXml());
            return doc;
        }

        #endregion // "Jobs Handlers"

		#region "Alert Handlers"

        /// <summary>
        /// Handle request to get the alerts list
        /// </summary>
        internal async Task HandleAgentAlertsRequest(AgentAlertsParams parameters, RequestContext<AgentAlertsResult> requestContext)
        {
            await Task.Run(async () =>
            {
                var result = new AgentAlertsResult();
                result.Alerts = new List<AgentAlertInfo>().ToArray();

                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);

                if (connInfo != null)
                {
                    CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);
                    AlertCollection alerts = dataContainer.Server.JobServer.Alerts;
                }

                await requestContext.SendResult(result);
            });
        }

        private bool ValidateAgentAlertInfo(AgentAlertInfo alert)
        {
            return alert != null
                && !string.IsNullOrWhiteSpace(alert.JobName);
        }

        /// <summary>
        /// Handle request to create an alert
        /// </summary>
        internal async Task HandleCreateAgentAlertRequest(CreateAgentAlertParams parameters, RequestContext<CreateAgentAlertResult> requestContext)
        {
            await Task.Run(async () =>
            {
                var result = new CreateAgentAlertResult();
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);

                CreateOrUpdateAgentAlert(connInfo, parameters.Alert);

                await requestContext.SendResult(result);
            });
        }

        /// <summary>
        /// Handle request to update an alert
        /// </summary>
        internal async Task HandleUpdateAgentAlertRequest(UpdateAgentAlertParams parameters, RequestContext<UpdateAgentAlertResult> requestContext)
        {
            await Task.Run(async () =>
            {
                var result = new UpdateAgentAlertResult();
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);

                CreateOrUpdateAgentAlert(connInfo, parameters.Alert);

                await requestContext.SendResult(result);
            });
        }

        private void CreateOrUpdateAgentAlert(ConnectionInfo connInfo, AgentAlertInfo alert)
        {
            if (connInfo != null && ValidateAgentAlertInfo(alert))
            {
                CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);
                STParameters param = new STParameters(dataContainer.Document);
                param.SetParam("alert", alert.JobName);

                using (AgentAlertActions agentAlert = new AgentAlertActions(dataContainer, alert))
                {
                    agentAlert.CreateOrUpdate();
                }
            }
        }

        /// <summary>
        /// Handle request to delete an alert
        /// </summary>
        internal async Task HandleDeleteAgentAlertRequest(DeleteAgentAlertParams parameters, RequestContext<ResultStatus> requestContext)
        {
            await Task.Run(async () =>
            {
                var result = new ResultStatus();
                try
                {
                    ConnectionInfo connInfo;
                    ConnectionServiceInstance.TryFindConnection(
                        parameters.OwnerUri,
                        out connInfo);

                    AgentAlertInfo alert = parameters.Alert;
                    if (connInfo != null && ValidateAgentAlertInfo(alert))
                    {
                        CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);
                        STParameters param = new STParameters(dataContainer.Document);
                        param.SetParam("alert", alert.JobName);

                        using (AgentAlertActions agentAlert = new AgentAlertActions(dataContainer, alert))
                        {
                            agentAlert.Drop();
                            result.Success = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = ex.ToString();
                }

                await requestContext.SendResult(result);
            });
        }

        #endregion // "Alert Handlers"

        #region "Operator Handlers"

        internal async Task HandleAgentOperatorsRequest(AgentOperatorsParams parameters, RequestContext<AgentOperatorsResult> requestContext)
        {
            await requestContext.SendResult(null);
        }

        internal async Task HandleCreateAgentOperatorRequest(CreateAgentOperatorParams parameters, RequestContext<CreateAgentOperatorResult> requestContext)
        {
            await Task.Run(async () =>
            {
                var result = new CreateAgentOperatorResult();
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);

                AgentOperatorInfo operatorInfo = parameters.Operator;
                CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);
                STParameters param = new STParameters(dataContainer.Document);
                param.SetParam("operator", operatorInfo.Name);

                using (AgentOperator agentOperator = new AgentOperator(dataContainer, operatorInfo))
                {
                    agentOperator.CreateOrUpdate();
                }

                await requestContext.SendResult(result);
            });
        }

        internal async Task HandleUpdateAgentOperatorRequest(UpdateAgentOperatorParams parameters, RequestContext<UpdateAgentOperatorResult> requestContext)
        {
            await requestContext.SendResult(null);
        }

        internal async Task HandleDeleteAgentOperatorRequest(DeleteAgentOperatorParams parameters, RequestContext<DeleteAgentOperatorResult> requestContext)
        {
            await requestContext.SendResult(null);
        }

        #endregion // "Operator Handlers"


        #region "Proxy Handlers"

        internal async Task HandleAgentProxiesRequest(AgentProxiesParams parameters, RequestContext<AgentProxiesResult> requestContext)
        {
            await requestContext.SendResult(null);
        }

        internal async Task HandleCreateAgentProxyRequest(CreateAgentProxyParams parameters, RequestContext<CreateAgentProxyResult> requestContext)
        {
             bool succeeded = await ConfigureAgentProxy(
                parameters.OwnerUri,
                parameters.Proxy.AccountName,
                parameters.Proxy,
                ConfigAction.Create);

            await requestContext.SendResult(new CreateAgentProxyResult()
            {
                Success = succeeded
            });
        }

        internal async Task HandleUpdateAgentProxyRequest(UpdateAgentProxyParams parameters, RequestContext<UpdateAgentProxyResult> requestContext)
        {
            bool succeeded = await ConfigureAgentProxy(
                parameters.OwnerUri,
                parameters.OriginalProxyName,
                parameters.Proxy,
                ConfigAction.Update);

            await requestContext.SendResult(new UpdateAgentProxyResult()
            {
                Success = succeeded
            });
        }

        internal async Task HandleDeleteAgentProxyRequest(DeleteAgentProxyParams parameters, RequestContext<ResultStatus> requestContext)
        {
            bool succeeded = await ConfigureAgentProxy(
                parameters.OwnerUri,
                parameters.Proxy.AccountName,
                parameters.Proxy,
                ConfigAction.Drop);

            await requestContext.SendResult(new ResultStatus()
            {
                Success = succeeded
            });
        }

        internal async Task<bool> ConfigureAgentProxy(
            string ownerUri,
            string accountName,
            AgentProxyInfo proxy,
            ConfigAction configAction)
        {
            return await Task<bool>.Run(() =>
            {
                try
                {
                    ConnectionInfo connInfo;
                    ConnectionServiceInstance.TryFindConnection(
                        ownerUri,
                        out connInfo);

                    CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);
                    STParameters param = new STParameters(dataContainer.Document);
                    param.SetParam("proxyaccount", accountName);

                    using (AgentProxyAccount agentProxy = new AgentProxyAccount(dataContainer, proxy))
                    {
                        if (configAction == ConfigAction.Create)
                        {
                            return agentProxy.Create();
                        }
                        else if (configAction == ConfigAction.Update)
                        {
                            return agentProxy.Update();
                        }
                        else if (configAction == ConfigAction.Drop)
                        {
                            return agentProxy.Drop();
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                catch (Exception)
                {
                    // log exception here
                    return false;
                }
            });
        }

        #endregion // "Proxy Handlers"
    }
}
