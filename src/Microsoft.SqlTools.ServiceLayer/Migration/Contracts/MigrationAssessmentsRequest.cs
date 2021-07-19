//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlServer.Migration.Assessment.Common.Contracts.Models;

namespace Microsoft.SqlTools.ServiceLayer.Migration.Contracts
{
    public class MigrationAssessmentsParams 
    {
        public string OwnerUri { get; set; }
        public string[] Databases { get; set; }
    }

    public class MigrationAssessmentResult
    {
        /// <summary>
        /// Errors that happen while running the assessment
        /// </summary>
        public ErrorModel[] Errors { get; set; }
        /// <summary>
        /// Result of the assessment
        /// </summary>
        public ServerAssessmentProperties AssessmentResult { get; set; }
        /// <summary>
        /// Start time of the assessment
        /// </summary>
        public string StartTime { get; set; }
        /// <summary>
        /// End time of the assessment
        /// </summary>
        public string EndedTime { get; set; }
        /// <summary>
        /// Contains the raw assessment response 
        /// </summary>
        public ISqlMigrationAssessmentModel RawAssessmentResult { get; set; }
    }

    /// <summary>
    /// Retreive metadata for the table described in the TableMetadataParams value
    /// </summary>
    public class MigrationAssessmentsRequest
    {
        public static readonly
            RequestType<MigrationAssessmentsParams, MigrationAssessmentResult> Type =
                RequestType<MigrationAssessmentsParams, MigrationAssessmentResult>.Create("migration/getassessments");
    }
}
