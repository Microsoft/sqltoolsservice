﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Data;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Extensibility;
using Microsoft.Kusto.ServiceLayer.DataSource.Metadata;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer.DataSourceModel
{
    /// <summary>
    /// A <see cref="DataSourceQuerier"/> handles SMO queries for one or more SMO object types.
    /// The <see cref="SupportedObjectTypes"/> property defines which types can be queried.
    /// 
    /// To query multiple 
    /// </summary>
    public abstract class DataSourceQuerier : IComposableService
    {
        private static object lockObject = new object();
        
        /// <summary>
        /// Queries SMO for a collection of objects using the <see cref="QueryContext"/> 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public abstract IEnumerable<DataSourceObjectMetadata> Query(QueryContext context, string filter, bool refresh, IEnumerable<string> extraProperties);

        internal IMultiServiceProvider ServiceProvider
        {
            get;
            private set;
        }

        public void SetServiceProvider(IMultiServiceProvider provider)
        {
            ServiceProvider = provider;
        }

        /// <summary>
        /// Convert the data to data reader is possible
        /// </summary>
        protected IDataReader GetDataReader(object data)
        {
            IDataReader reader = null;
            if (data is IDataReader)
            {
               
                reader = data as IDataReader;
            }
            else if(data is DataTable)
            {
                reader = ((DataTable)data).CreateDataReader();
            }
           
            else if (data is DataSet)
            {
                reader = ((DataSet)data).Tables[0].CreateDataReader();
            }

            return reader;
        }

        /// <summary>
        /// Mthod used to do custom filtering on smo objects if cannot be implemented using the filters
        /// </summary>
        protected virtual bool PassesFinalFilters(SqlSmoObject parent, SqlSmoObject objectMetadata)
        {
            return true;
        }

        /// <summary>
        /// Indicates which platforms the querier is valid for
        /// </summary>
        public virtual ValidForFlag ValidFor
        {
            get
            {
                return ValidForFlag.All;
            }
        }
    }
}
