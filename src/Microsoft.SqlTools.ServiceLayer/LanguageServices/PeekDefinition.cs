//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    /// <summary>
    /// Peek Definition/ Go to definition implementation
    /// Script sql objects and write create scripts to file
    /// </summary>
    internal class PeekDefinition
    {
        private ConnectionInfo connectionInfo;
        private string tempPath;

        internal delegate StringCollection ScriptGetter(string objectName, string schemaName);
        
        // Dictionary that holds the script getter for each type
        private Dictionary<DeclarationType, ScriptGetter> sqlScriptGetters =
            new Dictionary<DeclarationType, ScriptGetter>();

        // Dictionary that holds the object name (as appears on the TSQL create statement)
        private Dictionary<DeclarationType, string> sqlObjectTypes = new Dictionary<DeclarationType, string>();

        private Database Database 
        {
            get
            {
                if (this.connectionInfo.SqlConnection != null)
                {
                    Server server = new Server(this.connectionInfo.SqlConnection.DataSource);
                    return server.Databases[this.connectionInfo.SqlConnection.Database];

                }
                return null;
            }
        }
        
        internal PeekDefinition(ConnectionInfo connInfo)
        {
            this.connectionInfo = connInfo;
            DirectoryInfo tempScriptDirectory = Directory.CreateDirectory(Path.GetTempPath() + "mssql_definition");
            this.tempPath = tempScriptDirectory.FullName;
            Initialize();
        }

        /// <summary>
        /// Add getters for each sql object supported by peek definition
        /// </summary>
        private void Initialize()
        {
            //Add script getters for each sql object

            //Add tables to supported types
            AddSupportedType(DeclarationType.Table, GetTableScripts, "Table");

            //Add views to supported types
            AddSupportedType(DeclarationType.View, GetViewScripts, "view");

            //Add stored procedures to supported types
            AddSupportedType(DeclarationType.StoredProcedure, GetStoredProcedureScripts, "Procedure");
        }

        /// <summary>
        /// Add the given type, scriptgetter and the typeName string to the respective dictionaries
        /// </summary>
        private void AddSupportedType(DeclarationType type, ScriptGetter scriptGetter, string typeName)
        {
            sqlScriptGetters.Add(type, scriptGetter);
            sqlObjectTypes.Add(type, typeName);

        }

        /// <summary>
        /// Convert a file to a location array containing a location object as expected by the extension
        /// </summary>
        private Location[] GetLocationFromFile(string tempFileName, int lineNumber)
        {
            Location[] locations = new[] { 
                    new Location {
                        Uri = new Uri(tempFileName).AbsoluteUri,
                        Range = new Range {
                            Start = new Position { Line = lineNumber, Character = 1},
                            End = new Position { Line = lineNumber + 1, Character = 1}
                        }
                    }
            };
            return locations;
        }

        /// <summary>
        /// Get line number for the create statement
        /// </summary>
        private int GetStartOfCreate(string script, string createString)
        {
            string[] lines = script.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            for (int lineNumber = 0; lineNumber < lines.Length; lineNumber++)
            {
                if (lines[lineNumber].IndexOf( createString, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return lineNumber;
                }
            }
            return 0;
        }

        /// <summary>
        /// Get the script of the selected token based on the type of the token
        /// </summary>
        /// <param name="declarationItems"></param>
        /// <param name="tokenText"></param>
        /// <param name="schemaName"></param>
        /// <returns>Location object of the script file</returns>
        internal Location[] GetScript(IEnumerable<Declaration> declarationItems, string tokenText, string schemaName)
        {
            foreach (Declaration declarationItem in declarationItems)
            {
                if (declarationItem.Title.Equals(tokenText))
                {
                    // Script object using SMO based on type
                    DeclarationType type  = declarationItem.Type;
                    if (sqlScriptGetters.ContainsKey(type) && sqlObjectTypes.ContainsKey(type))
                    {
                        return GetSqlObjectDefinition( 
                                    sqlScriptGetters[type], 
                                    tokenText, 
                                    schemaName, 
                                    sqlObjectTypes[type]
                                ); 
                    }
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// Script a table using SMO
        /// </summary>
        /// <param name="tableName">Table name</param>
        /// <param name="schemaName">Schema name</param>
        /// <returns>String collection of scripts</returns>
        internal StringCollection GetTableScripts(string tableName, string schemaName)
        {
            return (schemaName != null) ? Database?.Tables[tableName, schemaName]?.Script()
                    : Database?.Tables[tableName]?.Script();
        }

        /// <summary>
        /// Script a view using SMO
        /// </summary>
        /// <param name="viewName">View name</param>
        /// <param name="schemaName">Schema name </param>
        /// <returns>String collection of scripts</returns>
        internal StringCollection GetViewScripts(string viewName, string schemaName)
        {
            return (schemaName != null) ? Database?.Views[viewName, schemaName]?.Script()
                    : Database?.Views[viewName]?.Script();
        }

        /// <summary>
        /// Script a stored procedure using SMO
        /// </summary>
        /// <param name="storedProcedureName">Stored Procedure name</param>
        /// <param name="schemaName">Schema Name</param>
        /// <returns>String collection of scripts</returns>
        internal StringCollection GetStoredProcedureScripts(string viewName, string schemaName)
        {
            return (schemaName != null) ? Database?.StoredProcedures[viewName, schemaName]?.Script()
                    : Database?.StoredProcedures[viewName]?.Script();
        }

        /// <summary>
        /// Script a object using SMO and write to a file.
        /// </summary>
        /// <param name="sqlScriptGetter">Function that returns the SMO scripts for an object</param>
        /// <param name="objectName">SQL object name</param>
        /// <param name="schemaName">Schema name or null</param>
        /// <param name="objectType">Type of SQL object</param>
        /// <returns>Location object representing URI and range of the script file</returns>
        internal Location[] GetSqlObjectDefinition(
                ScriptGetter sqlScriptGetter, 
                string objectName, 
                string schemaName, 
                string objectType) 
        {
            if (this.connectionInfo.SqlConnection != null)
            {
                StringCollection scripts = sqlScriptGetter(objectName, schemaName);
                string tempFileName = (schemaName != null) ?  Path.Combine(this.tempPath, string.Format("{0}.{1}.sql", schemaName, objectName)) 
                                                    : Path.Combine(this.tempPath, string.Format("{0}.sql", objectName));

                if (scripts != null)
                {
                    int lineNumber = 0;
                    using (StreamWriter scriptFile = new StreamWriter(File.Open(tempFileName, FileMode.Create, FileAccess.ReadWrite)))
                    {
                        
                        foreach (string script in scripts)
                        {
                            string createSyntax = string.Format("CREATE {0}", objectType);
                            if (script.IndexOf(createSyntax, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                scriptFile.WriteLine(script);
                                lineNumber = GetStartOfCreate(script, createSyntax);
                            }                       
                        }         
                    }
                    return GetLocationFromFile(tempFileName, lineNumber);
                }
            }
            return null;
        }
    }
}