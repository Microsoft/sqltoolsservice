//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.Metadata.Contracts;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Scripting
{
    /// <summary>
    /// Main class for Scripting Service functionality
    /// </summary>
    public sealed class ScriptingService
    {    
        private const int ScriptingOperationTimeout = 60000;

        private static readonly Lazy<ScriptingService> LazyInstance = new Lazy<ScriptingService>(() => new ScriptingService());

        public static ScriptingService Instance => LazyInstance.Value;

        private static ConnectionService connectionService = null;        

        private static LanguageService languageServices = null;   

         /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal static ConnectionService ConnectionServiceInstance
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
        /// Internal for testing purposes only
        /// </summary>
        internal static LanguageService LanguageServiceInstance
        {
            get
            {
                if (languageServices == null)
                {
                    languageServices = LanguageService.Instance;
                }
                return languageServices;
            }
            set
            {
                languageServices = value;
            }
        }

        /// <summary>
        /// Initializes the Scripting Service instance
        /// </summary>
        /// <param name="serviceHost"></param>
        /// <param name="context"></param>
        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(ScriptingScriptAsRequest.Type, HandleScriptingScriptAsRequest);
        }

        /// <summary>
        /// Handle Script As Create requests
        /// </summary>
        private static string HandleScriptCreate(
            ConnectionInfo connInfo,
            ObjectMetadata metadata)
        {
            // get or create the current parse info object
            ScriptParseInfo parseInfo = LanguageServiceInstance.GetScriptParseInfo(connInfo.OwnerUri);
            if (Monitor.TryEnter(parseInfo.BuildingMetadataLock, LanguageService.BindingTimeout))
            {
                try
                {
                    QueueItem queueItem = LanguageServiceInstance.BindingQueue.QueueBindingOperation(
                        key: parseInfo.ConnectionKey,
                        bindingTimeout: ScriptingService.ScriptingOperationTimeout,
                        bindOperation: (bindingContext, cancelToken) =>
                        {
                            PeekDefinition peekDefinition = new PeekDefinition(bindingContext.ServerConnection, connInfo);
                            var results = peekDefinition.GetTableScripts(metadata.Name, metadata.Schema);
                            string script = string.Empty;
                            if (results != null) 
                            {                                
                                foreach (var result in results)
                                {
                                    script += result.ToString() + Environment.NewLine + Environment.NewLine;
                                }
                            }
                            return script;
                        });

                    queueItem.ItemProcessed.WaitOne();

                    return queueItem.GetResultAsT<string>();
                }
                finally
                {
                    Monitor.Exit(parseInfo.BuildingMetadataLock);
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Script create statements for metadata object
        /// </summary>
        private static string ScriptAsCreate(
            IBindingContext bindingContext,
            ConnectionInfo connInfo,
            ObjectMetadata metadata)
        {
            PeekDefinition peekDefinition = new PeekDefinition(bindingContext.ServerConnection, connInfo);
            StringCollection results = null;
            if (metadata.MetadataType == MetadataType.Table)
            {
                results = peekDefinition.GetTableScripts(metadata.Name, metadata.Schema);
            }
            else if (metadata.MetadataType == MetadataType.SProc)
            {
                results = peekDefinition.GetStoredProcedureScripts(metadata.Name, metadata.Schema);
            }
            else if (metadata.MetadataType == MetadataType.View)
            {
                results = peekDefinition.GetViewScripts(metadata.Name, metadata.Schema);
            }

            string script = string.Empty;
            if (results != null) 
            {                                
                foreach (var result in results)
                {
                    script += result.ToString() + Environment.NewLine + Environment.NewLine;
                }
            }
            return script;
        }

        private static string ScriptAsUpdate(
            IBindingContext bindingContext,
            ConnectionInfo connInfo,
            ObjectMetadata metadata)
        {
            PeekDefinition peekDefinition = new PeekDefinition(bindingContext.ServerConnection, connInfo);
            var results = peekDefinition.GetTableScripts(metadata.Name, metadata.Schema);
            string script = string.Empty;
            if (results != null) 
            {                                
                foreach (var result in results)
                {
                    script += result.ToString() + Environment.NewLine + Environment.NewLine;
                }
            }
            return script;
        }

        private static string ScriptAsInsert(
            IBindingContext bindingContext,
            ConnectionInfo connInfo,
            ObjectMetadata metadata)
        {
            PeekDefinition peekDefinition = new PeekDefinition(bindingContext.ServerConnection, connInfo);
            var results = peekDefinition.GetTableScripts(metadata.Name, metadata.Schema);
            string script = string.Empty;
            if (results != null) 
            {                                
                foreach (var result in results)
                {
                    script += result.ToString() + Environment.NewLine + Environment.NewLine;
                }
            }
            return script;
        }

        private static string ScriptAsDelete(
            IBindingContext bindingContext,
            ConnectionInfo connInfo,
            ObjectMetadata metadata)
        {
            PeekDefinition peekDefinition = new PeekDefinition(bindingContext.ServerConnection, connInfo);
            var results = peekDefinition.GetTableScripts(metadata.Name, metadata.Schema);
            string script = string.Empty;
            if (results != null) 
            {                                
                foreach (var result in results)
                {
                    script += result.ToString() + Environment.NewLine + Environment.NewLine;
                }
            }
            return script;
        }

        /// <summary>
        /// Handle Script As Update requests
        /// </summary>
        private static string HandleScriptOperation(
            ScriptOperation operation,
            ConnectionInfo connInfo,
            ObjectMetadata metadata)
        {
            // get or create the current parse info object
            ScriptParseInfo parseInfo = LanguageServiceInstance.GetScriptParseInfo(connInfo.OwnerUri);
            if (Monitor.TryEnter(parseInfo.BuildingMetadataLock, LanguageService.BindingTimeout))
            {
                try
                {
                    QueueItem queueItem = LanguageServiceInstance.BindingQueue.QueueBindingOperation(
                        key: parseInfo.ConnectionKey,
                        bindingTimeout: ScriptingService.ScriptingOperationTimeout,
                        bindOperation: (bindingContext, cancelToken) =>
                        {
                            if (operation == ScriptOperation.Select)
                            {                    
                                return string.Format(
                                    @"SELECT TOP 100 * " + Environment.NewLine + @"FROM {0}.{1}",
                                    metadata.Schema, metadata.Name);
                            }
                            else if (operation == ScriptOperation.Create)
                            {
                                return ScriptAsCreate(bindingContext, connInfo, metadata);
                            }
                            else if (operation == ScriptOperation.Update)
                            {
                                return ScriptAsUpdate(bindingContext, connInfo, metadata);
                            }
                            else if (operation == ScriptOperation.Insert)
                            {
                                return ScriptAsInsert(bindingContext, connInfo, metadata);
                            }
                            else if (operation == ScriptOperation.Delete)
                            {
                               return ScriptAsDelete(bindingContext, connInfo, metadata);
                            }
                            else
                            {
                                return null;
                            }
                        });

                    queueItem.ItemProcessed.WaitOne();

                    return queueItem.GetResultAsT<string>();
                }
                finally
                {
                    Monitor.Exit(parseInfo.BuildingMetadataLock);
                }
            }

            return string.Empty;
        }        

        /// <summary>
        /// Handles script as request messages
        /// </summary>
        /// <param name="scriptingParams"></param>
        /// <param name="requestContext"></param>
        internal static async Task HandleScriptingScriptAsRequest(
            ScriptingScriptAsParams scriptingParams,
            RequestContext<ScriptingScriptAsResult> requestContext)
        {
            try
            {
                ConnectionInfo connInfo;
                ScriptingService.ConnectionServiceInstance.TryFindConnection(
                    scriptingParams.OwnerUri,
                    out connInfo);

                ObjectMetadata metadata = scriptingParams.Metadata;
                string script = string.Empty;

                if (connInfo != null) 
                {
                    script = HandleScriptOperation(scriptingParams.Operation, connInfo, metadata);
                }

                await requestContext.SendResult(new ScriptingScriptAsResult
                {
                    OwnerUri = scriptingParams.OwnerUri,
                    Script = script
                });
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }
    }
}
