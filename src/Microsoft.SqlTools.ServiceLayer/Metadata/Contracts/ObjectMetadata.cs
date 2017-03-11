//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.Metadata.Contracts
{
    public enum MetadataType
    {
        Table = 0,
        View = 1,
        SProc = 2,
        Function = 3
    }

    public class ObjectMetadata 
    {
        public MetadataType MetadataType { get; set; }

        public string Schema { get; set; }

        public string ObjectName { get; set; }
    }
}
