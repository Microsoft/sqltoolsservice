﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.Cms.Contracts
{
    public class RegisteredServersResult
    {
        public List<RegisteredServerResult> RegisteredServersList { get; set; }
    }

    public class RegisteredServerResult
    {
        public string Name { get; set; }

        public string ServerName { get; set; }

        public string Description { get; set; }

        public ConnectionDetails connectionDetails { get; set; }
    }
}