﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Hosting.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ServiceHost
{
    public class ServiceHostTests
    {
        [Fact]
        public async Task InitializeResultShouldIncludeTheCharactersThatWouldTriggerTheCompletion()
        {
            Hosting.ServiceHost host = Hosting.ServiceHost.Instance;
            var requesContext = new Mock<RequestContext<InitializeResult>>();
            requesContext.Setup(x => x.SendResult(It.IsAny<InitializeResult>())).Returns(Task.FromResult(new object()));
            await host.HandleInitializeRequest(new InitializeRequest(), requesContext.Object);
            requesContext.Verify(x => x.SendResult(It.Is<InitializeResult>(
                i => i.Capabilities.CompletionProvider.TriggerCharacters.All(t => Hosting.ServiceHost.CompletionTriggerCharacters.Contains(t)))));
        }
    }
}
