//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Contracts;

namespace Microsoft.SqlTools.DataProtocol.Contracts.Connection
{
    /// <summary>
    /// List databases request mapping entry 
    /// </summary>
    public class ChangeDatabaseRequest
    {
        public static readonly
            RequestType<ChangeDatabaseParams, bool> Type =
            RequestType<ChangeDatabaseParams, bool>.Create("connection/changedatabase");
    }
}
