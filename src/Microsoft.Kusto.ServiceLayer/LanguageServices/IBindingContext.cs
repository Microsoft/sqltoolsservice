//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SmoMetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.Kusto.ServiceLayer.DataSource;


namespace Microsoft.Kusto.ServiceLayer.LanguageServices
{
    /// <summary>
    /// The context used for binding requests
    /// </summary>
    public interface IBindingContext
    {
        /// <summary>
        /// Gets or sets a flag indicating if the context is connected
        /// </summary>
        bool IsConnected { get; set; }

        /// <summary>
        /// Gets or sets the binding server connection
        /// </summary>
        ServerConnection ServerConnection { get; set; }

        /// <summary>
        /// Gets or sets data source interface
        /// </summary>
        IDataSource DataSource { get; set; }

        /// <summary>
        /// Gets or sets the metadata display info provider
        /// </summary>
        MetadataDisplayInfoProvider MetadataDisplayInfoProvider { get; set; }

        /// <summary>
        /// Gets or sets the SMO metadata provider
        /// </summary>
        SmoMetadataProvider SmoMetadataProvider { get; set; }

        /// <summary>
        /// Gets or sets the binder
        /// </summary>
        IBinder Binder { get; set; }

        /// <summary>
        /// Gets the binding lock object
        /// </summary>
        ManualResetEvent BindingLock { get; }

        /// <summary>
        /// Gets or sets the binding operation timeout in milliseconds
        /// </summary>
        int BindingTimeout { get; set; }
    }
}
