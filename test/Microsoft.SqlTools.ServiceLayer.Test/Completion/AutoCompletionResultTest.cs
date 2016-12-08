﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System.Collections.Generic;
using System.Threading;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.Completion
{
    public class AutoCompletionResultTest
    {
        [Fact]
        public void MetricsShouldGetSortedGivenUnSortedArray()
        {
            AutoCompletionResult result = new AutoCompletionResult();
            int duration = 2000;
            Thread.Sleep(duration);
            result.CompleteResult(new List<Declaration>(), new CompletionItem[] { });
            Assert.True(result.Duration >= duration);
        }
    }
}
