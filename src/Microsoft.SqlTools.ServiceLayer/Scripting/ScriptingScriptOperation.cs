﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.SqlScriptPublish;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Scripting
{
    /// <summary>
    /// Class to represent an in-progress script operation.
    /// </summary>
    public sealed class ScriptingScriptOperation : ScriptingOperation
    {
        private CancellationTokenSource cancellation = new CancellationTokenSource();

        private bool disposed = false;

        private int scriptedObjectCount = 0;

        private int totalScriptedObjectCount = 0;

        private ScriptingParams Parameters { get; set; }

        private RequestContext<ScriptingResult> RequestContext { get; set; }

        public ScriptingScriptOperation(ScriptingParams parameters, RequestContext<ScriptingResult> requestContext)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException("parameters");
            }

            if (requestContext == null)
            {
                throw new ArgumentNullException("requestContext");
            }

            this.OperationId = Guid.NewGuid().ToString();
            this.Parameters = parameters;
            this.RequestContext = requestContext;
        }

        public Task ActiveTask { get; private set; }

        public override Task Execute()
        {
            string operationId = Guid.NewGuid().ToString();
            this.ActiveTask = Task.Run(() => this.InternalExecute(), this.cancellation.Token);
            return this.ActiveTask;
        }

        private void InternalExecute()
        {
            if (this.cancellation.IsCancellationRequested)
            {
                throw new OperationCanceledException(this.cancellation.Token);
            }

            try
            {
                this.ValidateScriptDatabaseParams();

                SqlScriptPublishModel publishModel = BuildPublishModel();
                ScriptOutputOptions outputOptions = new ScriptOutputOptions
                {
                    SaveFileMode = ScriptFileMode.Overwrite,
                    SaveFileType = ScriptFileType.Unicode,
                    SaveFileName = this.Parameters.FilePath,
                };

                publishModel.ScriptItemsCollected += this.OnPublishModelScriptItemsCollected;
                publishModel.ScriptProgress += this.OnPublishModelScriptProgress;
                publishModel.ScriptError += this.OnPublishModelScriptError;

                try
                {
                    publishModel.GenerateScript(outputOptions);
                }
                finally
                {
                    publishModel.ScriptItemsCollected -= this.OnPublishModelScriptItemsCollected;
                    publishModel.ScriptProgress -= this.OnPublishModelScriptProgress;
                    publishModel.ScriptError -= this.OnPublishModelScriptError;
                }

                this.SendJsonRpcEventAsync(
                    ScriptingCompleteEvent.Type, 
                    new ScriptingCompleteParameters { OperationId = this.OperationId });
            }
            catch (Exception e)
            {
                if (e.IsOperationCanceledException())
                {
                    this.SendJsonRpcEventAsync(
                        ScriptingCancelEvent.Type, 
                        new ScriptingCancelParameters { OperationId = this.OperationId });
                }
                else
                {
                    ScriptingErrorParams eventParams = new ScriptingErrorParams
                    {
                        OperationId = this.OperationId,
                        Message = e.Message,
                        DiagnosticMessage = e.ToString(),
                    };

                    this.SendJsonRpcEventAsync(ScriptingErrorEvent.Type, eventParams);
                }
            }
        }

        private SqlScriptPublishModel BuildPublishModel()
        {
            SqlScriptPublishModel publishModel = new SqlScriptPublishModel(this.Parameters.ConnectionString);
            PopulateAdvancedScriptOptions(publishModel.AdvancedOptions);
            publishModel.ScriptAllObjects = true;

            IEnumerable<ScriptingObject> selectedObjects = new List<ScriptingObject>();

            bool hasIncludeCriteria = this.Parameters.IncludeObjectCriteria != null && this.Parameters.IncludeObjectCriteria.Count > 0;
            bool hasExcludeCriteria = this.Parameters.ExcludeObjectCriteria != null && this.Parameters.ExcludeObjectCriteria.Count > 0;
            if (hasIncludeCriteria || hasExcludeCriteria)
            {
                List<ScriptingObject> databaseObjects = publishModel.GetDatabaseObjects();
                selectedObjects = ScriptingObjectMatchProcessor.Match(
                    this.Parameters.IncludeObjectCriteria,
                    this.Parameters.ExcludeObjectCriteria,
                    databaseObjects);
            }

            if (this.Parameters.DatabaseObjects != null && this.Parameters.DatabaseObjects.Count > 0)
            {
                selectedObjects = selectedObjects.Union(this.Parameters.DatabaseObjects);
            }

            if (selectedObjects.Count() > 0)
            {
                Logger.Write(
                    LogLevel.Normal,
                    string.Format("ScriptingOperation.BuildPublishModel scripting objects: {0}", string.Join(",", selectedObjects)));

                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(this.Parameters.ConnectionString);
                string server = builder.DataSource;
                string database = builder.InitialCatalog;
                foreach (ScriptingObject scriptingObject in selectedObjects)
                {
                    publishModel.SelectedObjects.Add(scriptingObject.ToUrn(server, database));
                }

                publishModel.ScriptAllObjects = false;
            }

            return publishModel;
        }

        private void PopulateAdvancedScriptOptions(SqlScriptOptions advancedOptions)
        {
            foreach (PropertyInfo optionPropInfo in this.Parameters.ScriptOptions.GetType().GetProperties())
            {
                string optionName = optionPropInfo.Name;
                object optionValue = optionPropInfo.GetValue(this.Parameters.ScriptOptions, index: null);
                string optionStringValue = optionValue != null ? optionValue.ToString() : null;
                if (optionStringValue != null)
                {
                    PropertyInfo advancedOptionPropInfo = advancedOptions.GetType().GetProperty(optionName);
                    if (advancedOptionPropInfo != null)
                    {
                        try
                        {
                            advancedOptionPropInfo.SetValue(advancedOptions, Enum.Parse(advancedOptionPropInfo.PropertyType, optionStringValue, ignoreCase: true));
                        }
                        catch (Exception e)
                        {
                            Logger.Write(
                                LogLevel.Warning, 
                                string.Format("ScriptingOperation.PopulateAdvancedScriptOptions exception {0} {1}: {2}", optionName, optionStringValue, e));
                        }
                    }              
                }              
            }
        }

        private void ValidateScriptDatabaseParams()
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(this.Parameters.ConnectionString);
            }
            catch (Exception e)
            {
                throw new ArgumentException("Error parsing ConnectionString property", e);
            }

            if (!Directory.Exists(Path.GetDirectoryName(this.Parameters.FilePath)))
            {
                throw new ArgumentException("Invalid directory specified by the FilePath property.");
            }
        }

        private void OnPublishModelScriptError(object sender, ScriptEventArgs e)
        {
            ScriptingErrorParams eventParams = new ScriptingErrorParams
            {
                OperationId = this.OperationId,
                Message = e.Error.Message,
                DiagnosticMessage = e.Error.ToString(),
            };

            this.SendJsonRpcEventAsync(ScriptingErrorEvent.Type, eventParams);
        }

        private void OnPublishModelScriptItemsCollected(object sender, ScriptItemsArgs e)
        {
            if (this.cancellation.IsCancellationRequested)
            {
                throw new OperationCanceledException(this.cancellation.Token);
            }

            List<ScriptingObject> scriptingObjects = e.Urns.Select(urn => urn.ToScriptingObject()).ToList();
            this.totalScriptedObjectCount = scriptingObjects.Count;

            ScriptingPlanNotificationParams eventParams = new ScriptingPlanNotificationParams
            {
                OperationId = this.OperationId,
                DatabaseObjects = scriptingObjects,
                Count = scriptingObjects.Count,
            };

            this.SendJsonRpcEventAsync(ScriptingPlanNotificationEvent.Type, eventParams);
        }

        private void OnPublishModelScriptProgress(object sender, ScriptEventArgs e)
        {
            if (this.cancellation.IsCancellationRequested)
            {
                e.ContinueScripting = false;
                throw new OperationCanceledException(this.cancellation.Token);
            }

            if (e.Completed)
            {
                this.scriptedObjectCount += 1;
            }

            // TODO: Handle the e.Error case.
            ScriptingProgressNotificationParams eventParams = new ScriptingProgressNotificationParams
            {
                OperationId = this.OperationId,
                ScriptingObject = e.Urn.ToScriptingObject(),
                Status = e.Completed ? "Completed" : "Progress",
                Count = this.scriptedObjectCount,
                TotalCount = this.totalScriptedObjectCount,
            };

            this.SendJsonRpcEventAsync(ScriptingProgressNotificationEvent.Type, eventParams);
        }

        public override void Cancel()
        {
            if (this.cancellation != null && !this.cancellation.IsCancellationRequested)
            {
                Logger.Write(LogLevel.Normal, string.Format("ScriptingOperation.Cancel invoked for OperationId {0}", this.OperationId));
                this.cancellation.Cancel();
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

        private void SendJsonRpcEventAsync<TParams>(EventType<TParams> eventType, TParams eventParams)
        {
            Task.Run(async () => await this.RequestContext.SendEvent(eventType, eventParams));
        }
    }
}
