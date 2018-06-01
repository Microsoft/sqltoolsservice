//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Agent.Contracts
{
    /// <summary>
    /// SQL Agent Job activity parameters
    /// </summary>
    public class AgentJobsParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public string JobId { get; set; }
    }

    /// <summary>
    /// SQL Agent Job activity result
    /// </summary>
    public class AgentJobsResult
    {

        public bool Succeeded { get; set; }

        public string ErrorMessage { get; set; }

        public AgentJobInfo[] Jobs { get; set; }
    }

    /// <summary>
    /// SQL Agent Jobs request type
    /// </summary>
    public class AgentJobsRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<AgentJobsParams, AgentJobsResult> Type =
            RequestType<AgentJobsParams, AgentJobsResult>.Create("agent/jobs");
    }

    /// <summary>
    /// SQL Agent create Job params
    /// </summary>
    public class CreateAgentJobParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public AgentJobInfo Job { get; set; }
    }

    /// <summary>
    /// SQL Agent create Job result
    /// </summary>
    public class CreateAgentJobResult
    {
        public bool Succeeded { get; set; }

        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// SQL Agent create Alert request type
    /// </summary>
    public class CreateAgentJobRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<CreateAgentJobParams, CreateAgentJobResult> Type =
            RequestType<CreateAgentJobParams, CreateAgentJobResult>.Create("agent/createjob");
    }

    /// <summary>
    /// SQL Agent delete Alert params
    /// </summary>
    public class DeleteAgentJobParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public AgentJobInfo Job { get; set; }
    }

    /// <summary>
    /// SQL Agent delete Job result
    /// </summary>
    public class DeleteAgentJobResult
    {
        public bool Succeeded { get; set; }

        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// SQL Agent delete Job request type
    /// </summary>
    public class DeleteAgentJobRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<DeleteAgentJobParams, DeleteAgentJobResult> Type =
            RequestType<DeleteAgentJobParams, DeleteAgentJobResult>.Create("agent/deletejob");
    }

    /// <summary>
    /// SQL Agent update Job params
    /// </summary>
    public class UpdateAgentJobParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public AgentJobInfo Job { get; set; }
    }

    /// <summary>
    /// SQL Agent update Job result
    /// </summary>
    public class UpdateAgentJobResult
    {
        public bool Succeeded { get; set; }

        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// SQL Agent update Job request type
    /// </summary>
    public class UpdateAgentJobRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<UpdateAgentJobParams, UpdateAgentJobResult> Type =
            RequestType<UpdateAgentJobParams, UpdateAgentJobResult>.Create("agent/updatejob");
    }

    /// <summary>
    /// SQL Agent Job history parameter
    /// </summary>
    public class AgentJobHistoryParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public string JobId { get; set; }
    }

    /// <summary>
    /// SQL Agent Job history result
    /// </summary>
    public class AgentJobHistoryResult
    {

        public bool Succeeded { get; set; }

        public string ErrorMessage { get; set; }

        public AgentJobHistoryInfo[] Jobs { get; set; }
    }

    /// <summary>
    /// SQL Agent Jobs request type
    /// </summary>
    public class AgentJobHistoryRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<AgentJobHistoryParams, AgentJobHistoryResult> Type =
            RequestType<AgentJobHistoryParams, AgentJobHistoryResult>.Create("agent/jobhistory");
    }

    /// <summary>
    /// SQL Agent Job activity parameters
    /// </summary>
    public class AgentJobActionParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public string JobName { get; set; }

        public string Action { get; set; }
    }

    /// <summary>
    /// SQL Agent Job activity result
    /// </summary>
    public class AgentJobActionResult
    {
        public bool Succeeded { get; set; }

        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// SQL Agent Jobs request type
    /// </summary>
    public class AgentJobActionRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<AgentJobActionParams, AgentJobActionResult> Type =
            RequestType<AgentJobActionParams, AgentJobActionResult>.Create("agent/jobaction");
    }
}
