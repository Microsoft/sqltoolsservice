﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SqlTools.Azure.Core.Authentication;

namespace Microsoft.SqlTools.Azure.Core.FirewallRule
{
    /// <summary>
    /// Includes azure resource and subscription needed to create firewall rule
    /// </summary>
    internal class FirewallRuleResource
    {
        /// <summary>
        /// Azure resource
        /// </summary>
        public IAzureSqlServerResource AzureResource { get; set; }

        /// <summary>
        /// Azure Subscription
        /// </summary>
        public IAzureUserAccountSubscriptionContext SubscriptionContext { get; set; }
               

        /// <summary>
        /// Returns true if the resource and subscription are not null
        /// </summary>
        public bool IsValid
        {
            get
            {
                return AzureResource != null && SubscriptionContext != null;
            }
        }
    }
}
