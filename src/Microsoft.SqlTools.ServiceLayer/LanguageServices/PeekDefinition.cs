//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.IO;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    public class PeekDefinition
    {
        private ConnectionInfo connectionInfo;
        private string tempPath;

        private Database database 
        {
            get
            {
                Server server = new Server(this.connectionInfo.SqlConnection.DataSource);
                return server.Databases[this.connectionInfo.SqlConnection.Database];
            }
        }

        public PeekDefinition(ConnectionInfo connInfo)
        {
            connectionInfo = connInfo;
            tempPath = Path.GetTempPath();
        }

        /// <summary>
        /// Script a table using SMO and write to a file.
        /// </summary>
        /// <param name="tableName">Table name</param>
        /// <returns>Location object representing URI and range of the script file</returns>
        internal Location[] GetTableDefinition(string tableName)
        {
            if (this.connectionInfo.SqlConnection != null)
            {
                Table table = database.Tables[tableName];
                string tempFileName = tempPath + tableName + ".sql";

                if (table != null)
                {
                    using (StreamWriter scriptFile = new StreamWriter(File.Open(tempFileName, FileMode.Create, FileAccess.ReadWrite)))
                    {
                        StringCollection scripts = table.Script();
                        foreach (string script in scripts)
                        {
                            if (script.Contains("CREATE TABLE"))
                            {
                                scriptFile.WriteLine(script);
                            }  
                        }

                        
                    }
                    return GetLocationFromFile(tempFileName);
                }
            }
            return null;
        }

        /// <summary>
        /// Script a view using SMO and write to a file.
        /// </summary>
        /// <param name="viewName">View name</param>
        /// <param name="schemaName">Schema name </param>
        /// <returns>Location object representing URI and range of the script file</returns>
        internal Location[] GetViewDefinition(string viewName, string schemaName)
        {
            if (this.connectionInfo.SqlConnection != null)
            {
                View view = (schemaName != null) ? database.Views[viewName, schemaName] : database.Views[viewName];
                string tempFileName = (schemaName != null) ? tempPath + schemaName + "." + viewName + ".sql"
                                                    : tempPath + viewName + ".sql";

                if (view != null)
                {
                    using (StreamWriter scriptFile = new StreamWriter(File.Open(tempFileName, FileMode.Create, FileAccess.ReadWrite)))
                    {
                        StringCollection scripts = view.Script();
                        foreach (string script in scripts)
                        {
                            if (script.Contains("CREATE VIEW"))
                            {
                                scriptFile.WriteLine(script);
                            }  
                        }
                        
                    }
                    return GetLocationFromFile(tempFileName);
                }
            }
            return null;
        }

        /// <summary>
        /// Script a stored procedure using SMO and write to a file.
        /// </summary>
        /// <param name="storedProcedureName">Stored Procedure name</param>
        /// <param name="schemaName">Schema Name</param>
        /// <returns>Location object representing URI and range of the script file</returns>
        internal Location[] GetStoredProcedureDefinition(string storedProcedureName, string schemaName)
        {
            if (this.connectionInfo.SqlConnection != null)
            {
                StoredProcedure storedProcedure = (schemaName != null) ? database.StoredProcedures[storedProcedureName, schemaName] :
                                                    database.StoredProcedures[storedProcedureName];
                string tempFileName = (schemaName != null) ? tempPath + schemaName + "." + storedProcedureName + ".sql" 
                                                    : tempPath + storedProcedureName + ".sql";

                if (storedProcedure != null)
                {
                    using (StreamWriter scriptFile = new StreamWriter(File.Open(tempFileName, FileMode.Create, FileAccess.ReadWrite)))
                    {
                        StringCollection scripts = storedProcedure.Script();
                        foreach (string script in scripts)
                        {
                            if (script.Contains("CREATE PROCEDURE"))
                            {
                                scriptFile.WriteLine(script);
                            }                       
                        }         
                    }
                    return GetLocationFromFile(tempFileName);
                }
            }
            return null;
        }

        /// <summary>
        /// Convert a file to a location array containing a location object as expected by the extension
        /// </summary>
        private Location[] GetLocationFromFile(string tempFileName)
        {
            Location[] locations = new[] { 
                    new Location {
                        Uri = new Uri(tempFileName).AbsoluteUri,
                        Range = new Range {
                            Start = new Position { Line = 0, Character = 1},
                            End = new Position { Line = 1, Character = 1}
                        }
                    }
            };
            return locations;
        }

    }
}