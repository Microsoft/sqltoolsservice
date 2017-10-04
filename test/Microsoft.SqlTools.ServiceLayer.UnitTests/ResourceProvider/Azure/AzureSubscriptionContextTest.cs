﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.SqlTools.ResourceProvider.DefaultImpl;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Azure
{
    /// <summary>
    /// Tests for AzureSubscriptionContextWrapper to verify the wrapper on azure subscription class
    /// </summary>
    public class AzureSubscriptionContextTest
    {
        [Fact]
        public void SubscriptionNameShouldReturnNullGivenNullSubscription()
        {
            AzureSubscriptionContext subscriptionContext = new AzureSubscriptionContext(null);
            Assert.True(subscriptionContext.SubscriptionName == String.Empty);
            Assert.True(subscriptionContext.Subscription == null);
        }

        [Fact]
        public void SubscriptionNameShouldReturnCorrectValueGivenValidSubscription()
        {
            string name = Guid.NewGuid().ToString();

            AzureSubscriptionContext subscriptionContext = new AzureSubscriptionContext(new AzureSubscriptionIdentifier(null, name, null));
            Assert.True(subscriptionContext.SubscriptionName == name);
            Assert.True(subscriptionContext.Subscription != null);
        }
    }
}
