//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Sdk.Sfc;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlTools.ServiceLayer.Admin;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
	/// <summary>
	/// AgentAlert class
	/// BUGBUG - plush - get rid of the corresponding resx file as it is not needed
	/// </summary>
	internal class AgentAlert : AgentControlBase
	{
		#region Members

        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;
        /// <summary>
        /// Agent alert name that is being edited
        /// </summary>
        private string  agentAlertName  = null;
       

#endregion

#region Constructors

        public AgentAlert()
        {
        }
        /// <summary>
        /// Default constructor that will be used to create dialog
        /// </summary>
        /// <param name="dataContainer"></param>
        public AgentAlert(CDataContainer dataContainer) : this()
        {
            try
            {
                CUtils              util        = new CUtils();
        

                this.DataContainer = dataContainer;
                STParameters    parameters  = new STParameters();

                parameters.SetDocument(dataContainer.Document);

                if (parameters.GetParam("alert", ref this.agentAlertName) == false)
                    this.agentAlertName = null;

                if (this.agentAlertName != null && this.agentAlertName.Length == 0)
                    this.agentAlertName = null;

                if (this.agentAlertName != null)
                {
                    Alert agentAlert = DataContainer.Server.JobServer.Alerts[this.agentAlertName];
                    if (agentAlert == null)
                        throw new ApplicationException("AgentAlertSR.AlertNotFound(this.agentAlertName)");
                }          
            }
            catch (Exception e)
            {
                // Wrap it up and go
                throw new ApplicationException("AgentAlertSR.FailedToCreateInitializeAgentAlertDialog", e);
            }
        }

#endregion

#region Overrides

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing )
        {
            if ( disposing )
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose( disposing );
        }

#endregion

#region implementation of the execution logic

        /// <summary>
        /// called by IExecutionAwareSqlControlCollection.PreProcessExecution to enable derived
        /// classes to take over execution of the dialog and do entire execution in this method
        /// rather than having the framework to execute dialog views one by one.
        /// 
        /// NOTE: it might be called from non-UI thread
        /// </summary>
        /// <param name="runType"></param>
        /// <param name="executionResult"></param>
        /// <returns>
        /// true if regular execution should take place, false if everything,
        /// has been done by this function
        /// </returns>
        protected override bool DoPreProcessExecution(RunType runType, out ExecutionMode executionResult)
        {
            base.DoPreProcessExecution(runType, out executionResult);

            Alert   alert = null;
            string alertName = "this.alertGeneral.AgentName.Trim()";
            if (alertName == null || alertName.Length == 0)
            {
                throw new Exception("SRError.AlertNameCannotBeBlank");
            }
            try
            {
                if (this.agentAlertName == null)
                {
                    if (this.DataContainer.Server.JobServer.Alerts.Contains(alertName))
                    {
                        this.DataContainer.Server.JobServer.Alerts.Refresh(); // Try to recover
                        if (this.DataContainer.Server.JobServer.Alerts.Contains(alertName)) // If still no luck
                            throw new ApplicationException("AgentAlertSR.AlertAlreadyExists(alertName)");
                    }

                    alert = new Alert(this.DataContainer.Server.JobServer, alertName);
                }
                else
                {
                    alert = this.DataContainer.Server.JobServer.Alerts[this.agentAlertName];
                }

                // Let pages modify alert's fields
                // this.alertGeneral.UpdateAlert(alert);
                // if (this.agentAlertName != null)
                //     this.agentAlertName = alert.Name;
                // this.alertResponse.UpdateAlert(alert);
                // this.alertOptions.UpdateAlert(alert);
                // if (this.alertHistory != null)
                //     this.alertHistory.UpdateAlert(alert);

                if (this.agentAlertName == null)
                    alert.Create();
                else
                {
                    // don't bother trying to update the alert unless they are sysadmin.   
                    // Note that there are checks elsewhere in the alert UI that make all 
                    // controls readonly if they are not sysadmin, so it's impossible that
                    // there are any changes to make anyway if this check fails.
                    if (!this.DataContainer.Server.ConnectionContext.IsInFixedServerRole(FixedServerRoles.SysAdmin))
                    {
                        return false;
                    }
                    alert.Alter();
                }


                // after alret is created or altered we can update operators
                //this.alertResponse.UpdateOperators(alert);

                // update the name in the xml document.
                STParameters param = new STParameters(this.DataContainer.Document);
                param.SetParam("alert", alertName);

                //we do entire execution in this method, so there is no need for our host
                //to bother calling OnRunNow on our panels
                return false;
            }
            catch (Exception e)
            {
                ApplicationException applicationException;

                if (this.agentAlertName == null)
                    applicationException = new ApplicationException("AgentAlertSR.CannotCreateNewAlert", e);
                else
                    applicationException = new ApplicationException("AgentAlertSR.CannotAlterAlert", e);

                throw applicationException;
            }
        }

        /// <summary>
        /// called before dialog's host executes OnReset method on all panels in the dialog one by one
        /// NOTE: it might be called from worker thread
        /// </summary>
        /// <returns>
        /// true if regular execution should take place, false if everything
        /// has been done by this function
        /// </returns>
        /// <returns></returns>
        protected override bool DoPreProcessReset()
        {
            base.DoPreProcessReset();
            // Reset dialog to defaults in case of new alert mode and to values from server in case of
            // modify alert mode
            if (this.agentAlertName != null)
            {
                this.DataContainer.Server.JobServer.Refresh();
                Alert alert = this.DataContainer.Server.JobServer.Alerts[this.agentAlertName];
                alert.Refresh();
            }

            return true;//make host to call Reset on all panels
        }

#endregion

#region Private helpers

    

#endregion
    }
}


