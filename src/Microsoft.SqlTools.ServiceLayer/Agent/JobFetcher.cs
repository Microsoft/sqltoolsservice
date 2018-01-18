//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Text;
using System.Data;
using System.Globalization;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo.Agent;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    internal class JobFetcher
    {
        private Enumerator enumerator = null;
        private ServerConnection connection = null;
        private SMO.Server server = null;

        public JobFetcher(ServerConnection connection)
        {
            System.Diagnostics.Debug.Assert(connection != null, "ServerConnection is null");
            this.enumerator = new Enumerator();
            this.connection = connection;
            this.server = new SMO.Server(connection);
        }

        //
        // ServerConnection object should be passed from caller,
        // who gets it from CDataContainer.ServerConnection
        //
        public Dictionary<Guid, JobProperties> FetchJobs(JobActivityFilter filter)
        {
            string urn = server.JobServer.Urn.Value + "/Job";

            if (filter != null)
            {
                urn += filter.GetXPathClause();
                return FilterJobs(FetchJobs(urn), filter);
            }

            return FetchJobs(urn);
        }

        /// <summary>
        /// Filter Jobs that matches criteria specified in JobActivityFilter
        /// here we filter jobs by properties that enumerator doesn't
        /// support filtering on.
        /// $ISSUE - - DevNote: Filtering Dictionaries can be easily done with Linq and System.Expressions in .NET 3.5
        /// This requires re-design of current code and might impact functionality / performance due to newer dependencies
        /// We need to consider this change in future enhancements for Job Activity monitor
        /// </summary>
        /// <param name="unfilteredJobs"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        private Dictionary<Guid, JobProperties> FilterJobs(Dictionary<Guid, JobProperties> unfilteredJobs, 
                                           JobActivityFilter filter)
        {
            if (unfilteredJobs == null)
            {
                return null;
            }

            if (filter == null ||
                (filter is IFilterDefinition &&
                 ((filter as IFilterDefinition).Enabled == false ||
                  (filter as IFilterDefinition).IsDefault())))
            {
                return unfilteredJobs;
            }

            Dictionary<Guid, JobProperties> filteredJobs = new Dictionary<Guid, JobProperties>();
            
            // Apply Filter
            foreach (JobProperties jobProperties in unfilteredJobs.Values)
            {
                // If this job passed all filter criteria then include in filteredJobs Dictionary
                if (this.CheckIfNameMatchesJob(filter, jobProperties) &&
                    this.CheckIfCategoryMatchesJob(filter, jobProperties) &&
                    this.CheckIfEnabledStatusMatchesJob(filter, jobProperties) &&
                    this.CheckIfScheduledStatusMatchesJob(filter, jobProperties) &&
                    this.CheckIfJobStatusMatchesJob(filter, jobProperties) &&
                    this.CheckIfLastRunOutcomeMatchesJob(filter, jobProperties) &&
                    this.CheckIfLastRunDateIsGreater(filter, jobProperties) &&
                    this.CheckifNextRunDateIsGreater(filter, jobProperties) &&
                    this.CheckJobRunnableStatusMatchesJob(filter, jobProperties))
                {
                    filteredJobs.Add(jobProperties.JobID, jobProperties);
                }
            }

            return filteredJobs;
        }

        /// <summary>
        /// check if job runnable status in filter matches given job property
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="jobProperties"></param>
        /// <returns></returns>
        private bool CheckJobRunnableStatusMatchesJob(JobActivityFilter filter, JobProperties jobProperties)
        {
            bool isRunnableMatched = false;
            // filter based on job runnable
            switch (filter.Runnable)
            {
                // if All was selected, include in match
                case EnumThreeState.All:
                    isRunnableMatched = true;
                    break;

                // if Yes was selected, include only if job that is runnable
                case EnumThreeState.Yes:
                    if (jobProperties.Runnable)
                    {
                        isRunnableMatched = true;
                    }
                    break;

                // if Yes was selected, include only if job is not runnable
                case EnumThreeState.No:
                    if (!jobProperties.Runnable)
                    {
                        isRunnableMatched = true;
                    }
                    break;
            }
            return isRunnableMatched;
        }

        /// <summary>
        /// Check if next run date for given job property is greater than the one specified in the filter
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="jobProperties"></param>
        /// <returns></returns>
        private  bool CheckifNextRunDateIsGreater(JobActivityFilter filter, JobProperties jobProperties)
        {
            bool isNextRunOutDateMatched = false;
            // filter next run date
            if (filter.NextRunDate.Ticks == 0 ||
                jobProperties.NextRun >= filter.NextRunDate)
            {
                isNextRunOutDateMatched = true;
            }
            return isNextRunOutDateMatched;
        }

        /// <summary>
        /// Check if last run date for given job property is greater than the one specified in the filter
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="jobProperties"></param>
        /// <returns></returns>
        private bool CheckIfLastRunDateIsGreater(JobActivityFilter filter, JobProperties jobProperties)
        {
            bool isLastRunOutDateMatched = false;
            // filter last run date
            if (filter.LastRunDate.Ticks == 0 ||
                jobProperties.LastRun >= filter.LastRunDate)
            {
                isLastRunOutDateMatched = true;
            }

            return isLastRunOutDateMatched;
        }

        /// <summary>
        /// check if last run status filter matches given job property
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="jobProperties"></param>
        /// <returns></returns>
        private bool CheckIfLastRunOutcomeMatchesJob(JobActivityFilter filter, JobProperties jobProperties)
        {
            bool isLastRunOutcomeMatched = false;
            // filter - last run outcome
            if (filter.LastRunOutcome == EnumCompletionResult.All ||
                jobProperties.LastRunOutcome == (int)filter.LastRunOutcome)
            {
                isLastRunOutcomeMatched = true;
            }

            return isLastRunOutcomeMatched;
        }

        /// <summary>
        /// Check if job status filter matches given jobproperty
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="jobProperties"></param>
        /// <returns></returns>
        private  bool CheckIfJobStatusMatchesJob(JobActivityFilter filter, JobProperties jobProperties)
        {
            bool isStatusMatched = false;
            // filter - job run status
            if (filter.Status == EnumStatus.All ||
                jobProperties.CurrentExecutionStatus == (int)filter.Status)
            {
                isStatusMatched = true;
            }

            return isStatusMatched;
        }

        /// <summary>
        /// Check if job scheduled status filter matches job
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="jobProperties"></param>
        /// <returns></returns>
        private bool CheckIfScheduledStatusMatchesJob(JobActivityFilter filter, JobProperties jobProperties)
        {
            bool isScheduledMatched = false;
            // apply filter - if job has schedules or not
            switch (filter.Scheduled)
            {
                // if All was selected, include in match
                case EnumThreeState.All:
                    isScheduledMatched = true;
                    break;

                // if Yes was selected, include only if job has schedule
                case EnumThreeState.Yes:
                    if (jobProperties.HasSchedule)
                    {
                        isScheduledMatched = true;
                    }
                    break;

                // if Yes was selected, include only if job does not have schedule
                case EnumThreeState.No:
                    if (!jobProperties.HasSchedule)
                    {
                        isScheduledMatched = true;
                    }
                    break;
            }

            return isScheduledMatched;
        }

        /// <summary>
        /// Check if job enabled status matches job
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="jobProperties"></param>
        /// <returns></returns>
        private bool CheckIfEnabledStatusMatchesJob(JobActivityFilter filter, JobProperties jobProperties)
        {
            bool isEnabledMatched = false;
            // apply filter - if job was enabled or not
            switch (filter.Enabled)
            {
                // if All was selected, include in match
                case EnumThreeState.All:
                    isEnabledMatched = true;
                    break;

                // if Yes was selected, include only if job has schedule
                case EnumThreeState.Yes:
                    if (jobProperties.Enabled)
                    {
                        isEnabledMatched = true;
                    }
                    break;

                // if Yes was selected, include only if job does not have schedule
                case EnumThreeState.No:
                    if (!jobProperties.Enabled)
                    {
                        isEnabledMatched = true;
                    }
                    break;
            }

            return isEnabledMatched;
        }

        /// <summary>
        /// Check if a category matches given jobproperty
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="jobProperties"></param>
        /// <returns></returns>
        private  bool CheckIfCategoryMatchesJob(JobActivityFilter filter, JobProperties jobProperties)
        {
            bool isCategoryMatched = false;
            // Apply category filter if specified
            if (filter.Category.Length > 0)
            {
                //
                // we count it as a match if the job category contains 
                // a case-insensitive match for the filter string.
                //
                string jobCategory = jobProperties.Category.ToLower(CultureInfo.CurrentCulture);
                if (String.Compare(jobCategory, filter.Category.Trim().ToLower(CultureInfo.CurrentCulture), StringComparison.Ordinal) == 0)
                {
                    isCategoryMatched = true;
                }
            }
            else
            {
                // No category filter was specified
                isCategoryMatched = true;
            }

            return isCategoryMatched;
        }

        /// <summary>
        /// Check if name filter specified matches given jobproperty
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="jobProperties"></param>
        /// <returns></returns>
        private bool CheckIfNameMatchesJob(JobActivityFilter filter, JobProperties jobProperties)
        {
            bool isNameMatched = false;

            //
            // job name (can be comma-separated list)
            // we count it as a match if the job name contains 
            // a case-insensitive match for any of the filter strings.
            //
            if (filter.Name.Length > 0)
            {
                string jobname = jobProperties.Name.ToLower(CultureInfo.CurrentCulture);
                string[] jobNames = filter.Name.ToLower(CultureInfo.CurrentCulture).Split(',');
                int length = jobNames.Length;

                for (int j = 0; j < length; ++j)
                {
                    if (jobname.IndexOf(jobNames[j].Trim(), StringComparison.Ordinal) > -1)
                    {
                        isNameMatched = true;
                        break;
                    }
                }
            }
            else
            {
                // No name filter was specified
                isNameMatched = true;
            }

            return isNameMatched;
        }

        /// <summary>
        /// Fetch jobs for a given Urn
        /// </summary>
        /// <param name="urn"></param>
        /// <returns></returns>
        public Dictionary<Guid, JobProperties> FetchJobs(string urn)          
        {
            if(String.IsNullOrEmpty(urn))
            {
                throw new ArgumentNullException("urn");
            }

            Request request = new Request(); 
            request.Urn = urn;
            request.Fields = new string[] 
                {
                    "Name", 
                    "IsEnabled", 
                    "Category",
                    "CategoryID",
                    "CategoryType",
                    "CurrentRunStatus",
                    "CurrentRunStep",
                    "HasSchedule",
                    "HasStep",
                    "HasServer",
                    "LastRunDate",
                    "NextRunDate",
                    "LastRunOutcome",
                    "JobID"
                };

            DataTable dt = enumerator.Process(connection, request);

            int numJobs = dt.Rows.Count;
            if (numJobs == 0)
            {
                return null;
            }

            Dictionary<Guid, JobProperties> foundJobs = new Dictionary<Guid, JobProperties>(numJobs);

            for (int i = 0; i < numJobs; ++i)
            {
                JobProperties jobProperties = new JobProperties(dt.Rows[i]);
                foundJobs.Add(jobProperties.JobID, jobProperties);
            }

            return foundJobs;
        }
    }

    /// <summary>
    /// a class for storing various properties of agent jobs, 
    /// used by the Job Activity Monitor
    /// </summary>
    internal class JobProperties
    {
        private string name;
        private int currentExecutionStatus;
        private int lastRunOutcome;
        private string currentExecutionStep;
        private bool enabled;
        private bool hasTarget;
        private bool hasSchedule;
        private bool hasStep;
        private bool runnable;
        private string category;
        private int categoryID;
        private int categoryType;
        private DateTime lastRun;
        private DateTime nextRun;
        private Guid jobId;

        private JobProperties()
        {
        }

        public JobProperties(DataRow row)
        {
            System.Diagnostics.Debug.Assert(row["Name"]             != DBNull.Value, "Name is null!");
            System.Diagnostics.Debug.Assert(row["IsEnabled"]        != DBNull.Value, "IsEnabled is null!");
            System.Diagnostics.Debug.Assert(row["Category"]         != DBNull.Value, "Category is null!");
            System.Diagnostics.Debug.Assert(row["CategoryID"]       != DBNull.Value, "CategoryID is null!");
            System.Diagnostics.Debug.Assert(row["CategoryType"]     != DBNull.Value, "CategoryType is null!");
            System.Diagnostics.Debug.Assert(row["CurrentRunStatus"] != DBNull.Value, "CurrentRunStatus is null!");
            System.Diagnostics.Debug.Assert(row["CurrentRunStep"]   != DBNull.Value, "CurrentRunStep is null!");
            System.Diagnostics.Debug.Assert(row["HasSchedule"]      != DBNull.Value, "HasSchedule is null!");
            System.Diagnostics.Debug.Assert(row["HasStep"]          != DBNull.Value, "HasStep is null!");
            System.Diagnostics.Debug.Assert(row["HasServer"]        != DBNull.Value, "HasServer is null!");
            System.Diagnostics.Debug.Assert(row["LastRunOutcome"]   != DBNull.Value, "LastRunOutcome is null!");
            System.Diagnostics.Debug.Assert(row["JobID"]            != DBNull.Value, "JobID is null!");

            this.name                    = row["Name"].ToString();
            this.enabled                 = Convert.ToBoolean(row["IsEnabled"], CultureInfo.InvariantCulture);
            this.category                = row["Category"].ToString();
            this.categoryID              = Convert.ToInt32(row["CategoryID"], CultureInfo.InvariantCulture);
            this.categoryType            = Convert.ToInt32(row["CategoryType"], CultureInfo.InvariantCulture);
            this.currentExecutionStatus  = Convert.ToInt32(row["CurrentRunStatus"], CultureInfo.InvariantCulture);
            this.currentExecutionStep    = row["CurrentRunStep"].ToString();
            this.hasSchedule             = Convert.ToBoolean(row["HasSchedule"], CultureInfo.InvariantCulture);
            this.hasStep                 = Convert.ToBoolean(row["HasStep"], CultureInfo.InvariantCulture);
            this.hasTarget               = Convert.ToBoolean(row["HasServer"], CultureInfo.InvariantCulture);
            this.lastRunOutcome          = Convert.ToInt32(row["LastRunOutcome"], CultureInfo.InvariantCulture);
            this.jobId                   = Guid.Parse(row["JobID"].ToString()); ;

            // for a job to be runnable, it must:
            // 1. have a target server
            // 2. have some steps
            this.runnable = this.hasTarget && this.hasStep;

            if (row["LastRunDate"] != DBNull.Value)
            {
                this.lastRun = Convert.ToDateTime(row["LastRunDate"], CultureInfo.InvariantCulture);
            }

            if (row["NextRunDate"] != DBNull.Value)
            {
                this.nextRun = Convert.ToDateTime(row["NextRunDate"], CultureInfo.InvariantCulture);
            }
        }

        public bool Runnable
        {
            get{ return runnable;}
        }

        public string Name
        {
            get{ return name;}
        }

        public string Category
        {
            get{ return category;}
        }

        public int CategoryID
        {
            get{ return categoryID;}
        }

        public int CategoryType
        {
            get{ return categoryType;}
        }

        public int LastRunOutcome
        {
            get{ return lastRunOutcome;}
        }

        public int CurrentExecutionStatus
        {
            get{ return currentExecutionStatus;}
        }

        public string CurrentExecutionStep
        {
            get{ return currentExecutionStep;}
        }

        public bool Enabled
        {
            get{ return enabled;}
        }

        public bool HasTarget
        {
            get{ return hasTarget;}
        }

        public bool HasStep
        {
            get{ return hasStep;}
        }

        public bool HasSchedule
        {
            get{ return hasSchedule;}
        }

        public DateTime NextRun
        {
            get{ return nextRun;}
        }

        public DateTime LastRun
        {
            get{ return lastRun;}
        }

        public Guid JobID
        {
            get
            {
                return this.jobId;
            }
        }

#if false
        private Bitmap bmpRunning = null;
        private Bitmap bmpEnabled = null;
        private Bitmap bmpFailed = null;
        private Bitmap bmpDisabled = null;

        public Bitmap StatusBitmap
        {
            get
            {
                // if it's running, use a running icon
                if (this.CurrentExecutionStatus == Convert.ToInt32(JobExecutionStatus.Executing, CultureInfo.InvariantCulture))
                {
                    if (bmpRunning == null)
                    {
                        CUtils util = new CUtils();
                        Icon icon = util.LoadIcon("start_jobs.ico");
                        bmpRunning = icon.ToBitmap();
                    }
                    return bmpRunning;
                }

                // if it the job failed, use an error bitmap
                else if (lastRunOutcome == Convert.ToInt32(CompletionResult.Failed, CultureInfo.InvariantCulture))
                {
                    if (bmpFailed == null)
                    {
                        CUtils util = new CUtils();
                        Icon icon = util.LoadIcon("ProgressError.ico");
                        bmpFailed = icon.ToBitmap();
                    }
                    return bmpFailed;
                }

                // if it's not enabled, or is not runnable, use a disabled icon
                else if (this.Enabled == false || this.Runnable == false)
                {
                    if (bmpDisabled == null)
                    {
                        CUtils util = new CUtils();
                        Icon icon = util.LoadIcon("Job_Disabled.ico");
                        bmpDisabled = icon.ToBitmap();
                    }
                    return bmpDisabled;
                }

                // otherwise just use a standard job icon.
                else
                {
                    if (bmpEnabled == null)
                    {
                        CUtils util = new CUtils();
                        Icon icon = util.LoadIcon("jobs.ico");
                        bmpEnabled = icon.ToBitmap();
                    }
                    return bmpEnabled;
                }
            }
        }
#endif        
    }
}
