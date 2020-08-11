//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlAssessment.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Migration.Contracts
{
    public class MigrationAssessmentsParams 
    {
        public string OwnerUri { get; set; }

        public string TargetSku { get; set; }
    }

    /// <summary>
    /// Retreive metadata for the table described in the TableMetadataParams value
    /// </summary>
    public class MigrationAssessmentsRequest
    {
        public static readonly
            RequestType<MigrationAssessmentsParams, AssessmentResult<CheckInfo>> Type =
                RequestType<MigrationAssessmentsParams, AssessmentResult<CheckInfo>>.Create("migration/getassessmentresult");
    }
}
