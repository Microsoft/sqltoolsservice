﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Admin.Contracts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.SqlTools.ServiceLayer.Admin
{
    public class DatabaseTaskHelper
    {
        private DatabasePrototype prototype;

        private XmlDocument document;

        public CDataContainer DataContainer { get; set; }

        /// <summary>
        /// Expose database prototype to internal classes 
        /// </summary>
        public DatabasePrototype Prototype
        {
            get
            {
                return this.prototype;
            }
            set
            {
                this.prototype = value;
            }
        }

        public void CreateDatabase(CDataContainer context)
        {
            InitializeDataMembers(context);
        }

        private void InitializeDataMembers(CDataContainer context)
        {
            if (context != null)
            {
                this.DataContainer = context;
                this.document = context.Document;

                int majorVersionNumber = context.Server.Information.Version.Major;
                Version sql2000sp3 = new Version(8, 0, 760);
                Version sql2005sp2 = new Version(9, 0, 3000);

                if (context.Server.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
                {
                    this.prototype = new DatabasePrototypeAzure(context);
                }
                else if (Utils.IsSql11OrLater(context.Server.Version.Major))
                {
                    this.prototype = new DatabasePrototype110(context);
                }
                else if (majorVersionNumber == 10)
                {
                    this.prototype = new DatabasePrototype100(context);
                }
                else if ((sql2005sp2 <= context.Server.Information.Version) &&
                    (context.Server.Information.EngineEdition == Edition.EnterpriseOrDeveloper))
                {
                    this.prototype = new DatabasePrototype90EnterpriseSP2(context);
                }
                else if (8 < majorVersionNumber)
                {
                    this.prototype = new DatabasePrototype90(context);
                }
                else if (sql2000sp3 <= context.Server.Information.Version)
                {
                    this.prototype = new DatabasePrototype80SP3(context);
                }
                else if (7 < majorVersionNumber)
                {
                    this.prototype = new DatabasePrototype80(context);
                }
                else
                {
                    this.prototype = new DatabasePrototype(context);
                }

                this.prototype.Initialize();

                //this.databasesCreated = new ArrayList();                
            }
            else
            {
                this.DataContainer = null;
                this.document = null;
                this.prototype = null;
                //this.databasesCreated = null;
            }
        }

        internal static DatabaseInfo DatabasePrototypeToDatabaseInfo(DatabasePrototype prototype)
        {
            var databaseInfo = new DatabaseInfo();
            databaseInfo.Options.Add(AdminServicesProviderOptionsHelper.Name, prototype.Name);
            databaseInfo.Options.Add(AdminServicesProviderOptionsHelper.Owner, prototype.Owner);
            databaseInfo.Options.Add(AdminServicesProviderOptionsHelper.Collation, prototype.Collation);
            
            for (int i = 0; i < prototype.Filegroups.Count; ++i)
            {
                var fileGroup = prototype.Filegroups[i];
                string itemPrefix = AdminServicesProviderOptionsHelper.FileGroups + "." + i + ".";
                databaseInfo.Options.Add(itemPrefix + AdminServicesProviderOptionsHelper.Name, fileGroup.Name);
             
            }

            for (int i = 0; i < prototype.Files.Count; ++i)
            {
                var file = prototype.Files[i];
                string itemPrefix = AdminServicesProviderOptionsHelper.DatabaseFiles + "." + i + ".";
                databaseInfo.Options.Add(itemPrefix + AdminServicesProviderOptionsHelper.Name, file.Name);
                databaseInfo.Options.Add(itemPrefix + AdminServicesProviderOptionsHelper.PhysicalName, file.PhysicalName);
            }

            return databaseInfo;
        }
    }
}
