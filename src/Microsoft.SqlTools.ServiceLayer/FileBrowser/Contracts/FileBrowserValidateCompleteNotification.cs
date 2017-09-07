﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.FileBrowser.Contracts
{
    /// <summary>
    /// Event params for validation completion
    /// </summary>
    public class FileBrowserValidateCompleteParams
    {
        /// <summary>
        /// Result of the operation
        /// </summary>
        public bool Succeeded;

        /// <summary>
        /// Error message if any
        /// </summary>
        public string Message;
    }

    /// <summary>
    /// Notification for validation completion
    /// </summary>
    public class FileBrowserValidateCompleteNotification
    {
        public static readonly
            EventType<FileBrowserValidateCompleteParams> Type =
            EventType<FileBrowserValidateCompleteParams>.Create("filebrowser/validatecomplete");
    }

}
